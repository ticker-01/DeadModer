import os
import re
import shutil
import struct
import tempfile
import sys
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import vpk

DEFAULT_GAME_PATHS = [
    Path(r"D:\Program Files\Steam\steamapps\common\Deadlock"),
    Path(r"C:\Program Files (x86)\Steam\steamapps\common\Deadlock"),
    Path(r"C:\Program Files\Steam\steamapps\common\Deadlock"),
    Path(r"D:\SteamLibrary\steamapps\common\Deadlock"),
    Path(r"E:\SteamLibrary\steamapps\common\Deadlock"),
]

STRIP_PATTERNS = (
    "scripts/heroes.vdata_c",
    "scripts/abilities.vdata_c",
    "scripts/generic_data.vdata_c",
    "scripts/misc.vdata_c",
)

ATTACHMENT_KEYS = (
    b"camera",
    b"Camera",
    b"attach_camera",
    b"eye_pt",
    b"chest",
    b"healthbar",
)


def find_deadlock_dir(custom_path: str | None = None) -> Path:
    if custom_path:
        p = Path(custom_path)
        if (p / "game" / "citadel" / "pak01_dir.vpk").exists():
            return p
    for path in DEFAULT_GAME_PATHS:
        if (path / "game" / "citadel" / "pak01_dir.vpk").exists():
            return path
    raise FileNotFoundError("Deadlock directory not found. Specify path manually.")


def patch_gameinfo(citadel_dir: Path) -> bool:
    gi_path = citadel_dir / "gameinfo.gi"
    if not gi_path.exists():
        return False

    try:
        content = gi_path.read_text(encoding="utf-8", errors="ignore")
    except Exception as e:
        print(f"[!] Warning: Cannot read gameinfo.gi: {e}")
        return False

    if "citadel/addons" in content:
        return False

    pattern = re.compile(r"(SearchPaths\s*\{)", re.IGNORECASE)
    new_content, count = pattern.subn(r"\1\n\t\t\tGame\t\t\t\tcitadel/addons", content, count=1)
    if count > 0:
        try:
            shutil.copy2(gi_path, gi_path.with_suffix(".gi.bak"))
            gi_path.write_text(new_content, encoding="utf-8")
            return True
        except PermissionError:
            print("[!] Warning: Cannot write to gameinfo.gi (Deadlock might be running). Close Deadlock to update gameinfo.gi.")
            return False
        except Exception as e:
            print(f"[!] Warning: Failed to patch gameinfo.gi: {e}")
            return False
    return False


def parse_s2_blocks(data: bytes) -> dict[bytes, tuple[int, int]]:
    if len(data) < 16:
        return {}
    _, version, _, block_offset, block_count = struct.unpack_from("<IHHII", data, 0)
    if version != 12 or block_count_invalid(block_count, len(data)):
        return {}

    blocks = {}
    entry_pos = 8 + block_offset
    for _ in range(block_count):
        if entry_pos + 12 > len(data):
            break
        b_type = data[entry_pos:entry_pos + 4]
        rel_offset, b_size = struct.unpack_from("<iI", data, entry_pos + 4)
        abs_offset = (entry_pos + 4) + rel_offset
        if 0 <= abs_offset <= len(data) - b_size:
            blocks[b_type] = (abs_offset, b_size)
        entry_pos += 12
    return blocks


def block_count_invalid(count: int, total_len: int) -> bool:
    return count == 0 or count > 64 or (count * 12 + 16) > total_len


def sync_vmdl_binary_in_place(mod_bytes: bytes, vanilla_bytes: bytes) -> tuple[bytes, bool]:
    mod_blocks = parse_s2_blocks(mod_bytes)
    van_blocks = parse_s2_blocks(vanilla_bytes)
    if not mod_blocks or not van_blocks:
        return mod_bytes, False

    buf = bytearray(mod_bytes)
    modified = False

    for tag in (b"CTRL", b"MDAT", b"DATA"):
        if tag not in mod_blocks or tag not in van_blocks:
            continue
        m_off, m_size = mod_blocks[tag]
        v_off, v_size = van_blocks[tag]
        van_slice = vanilla_bytes[v_off:v_off + v_size]

        if m_size == v_size and buf[m_off:m_off + m_size] != van_slice:
            if tag == b"CTRL":
                buf[m_off:m_off + m_size] = van_slice
                modified = True
                continue

        mod_slice = buf[m_off:m_off + m_size]
        for key in ATTACHMENT_KEYS:
            m_idx = mod_slice.find(key)
            v_idx = van_slice.find(key)
            if m_idx != -1 and v_idx != -1:
                window = 64
                if m_idx + window <= m_size and v_idx + window <= v_size:
                    if mod_slice[m_idx:m_idx + window] != van_slice[v_idx:v_idx + window]:
                        buf[m_off + m_idx:m_off + m_idx + window] = van_slice[v_idx:v_idx + window]
                        modified = True

    return bytes(buf), modified


def update_single_mod_vpk(
    vpk_path: Path,
    vanilla_pak_path: Path,
    vanilla_index: set[str],
    backup_dir: Path,
) -> tuple[str, str]:
    try:
        mod_pak = vpk.open(str(vpk_path))
        vanilla_pak = vpk.open(str(vanilla_pak_path))
    except Exception as e:
        return vpk_path.name, f"ERROR: {e}"

    mod_files = list(mod_pak)
    if not mod_files:
        return vpk_path.name, "SKIPPED (empty)"

    changed = False
    extracted_files: dict[str, bytes] = {}

    for filepath in mod_files:
        norm_path = filepath.replace("\\", "/").lower()

        # 1. Strip outdated gameplay scripts that break new patches
        if norm_path in STRIP_PATTERNS or (norm_path.startswith("scripts/") and norm_path.endswith(".vdata_c")):
            changed = True
            continue

        raw_data = mod_pak.get_file(filepath).read()

        # 2. Update skeleton files directly from vanilla VPK if present
        if norm_path.endswith(".vnmskel_c") and norm_path in vanilla_index:
            van_data = vanilla_pak.get_file(norm_path).read()
            if raw_data != van_data:
                raw_data = van_data
                changed = True

        # 3. Patch compiled models (.vmdl_c) against current vanilla models
        elif norm_path.endswith(".vmdl_c") and norm_path in vanilla_index:
            van_data = vanilla_pak.get_file(norm_path).read()
            patched_data, was_patched = sync_vmdl_binary_in_place(raw_data, van_data)
            if was_patched:
                raw_data = patched_data
                changed = True

        extracted_files[norm_path] = raw_data

    if not changed:
        return vpk_path.name, "OK (already up to date)"

    backup_dir.mkdir(parents=True, exist_ok=True)
    backup_target = backup_dir / vpk_path.name
    if not backup_target.exists():
        shutil.copy2(vpk_path, backup_target)

    with tempfile.TemporaryDirectory() as tmp_dir:
        tmp_root = Path(tmp_dir) / "content"
        for rel_path, file_bytes in extracted_files.items():
            out_file = tmp_root / rel_path
            out_file.parent.mkdir(parents=True, exist_ok=True)
            out_file.write_bytes(file_bytes)

        temp_vpk_out = Path(tmp_dir) / vpk_path.name
        new_pak = vpk.new(str(tmp_root))
        new_pak.save(str(temp_vpk_out))
        shutil.move(str(temp_vpk_out), str(vpk_path))

    return vpk_path.name, "UPDATED"


def run_updater(custom_deadlock_dir: str | None = None, max_workers: int = 8) -> None:
    game_dir = find_deadlock_dir(custom_deadlock_dir)
    citadel_dir = game_dir / "game" / "citadel"
    addons_dir = citadel_dir / "addons"
    vanilla_pak_path = citadel_dir / "pak01_dir.vpk"
    backup_dir = citadel_dir / "addons_backup"

    if patch_gameinfo(citadel_dir):
        print("[+] Restored SearchPaths in gameinfo.gi")
    else:
        print("[=] gameinfo.gi checked")

    if not addons_dir.exists():
        addons_dir.mkdir(parents=True, exist_ok=True)
        print("[!] Created empty addons directory. Place pakXX_dir.vpk mods there.")
        return

    mod_vpks = sorted(list(addons_dir.glob("pak*_dir.vpk")) + list(addons_dir.glob("pak*_dir.vpk.disabled")))
    if not mod_vpks:
        print("[=] No pakXX_dir.vpk files found in addons.")
        return

    print(f"[*] Indexing vanilla {vanilla_pak_path.name}...")
    vanilla_pak = vpk.open(str(vanilla_pak_path))
    vanilla_index = {f.replace("\\", "/").lower() for f in vanilla_pak}
    print(f"[*] Indexed {len(vanilla_index)} vanilla files. Scanning {len(mod_vpks)} mods...")

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(
                update_single_mod_vpk,
                mod_vpk,
                vanilla_pak_path,
                vanilla_index,
                backup_dir,
            ): mod_vpk
            for mod_vpk in mod_vpks
        }
        for future in as_completed(futures):
            vpk_name, status = future.result()
            print(f"[{status}] {vpk_name}")


if __name__ == "__main__":
    arg_dir = sys.argv[1] if len(sys.argv) > 1 else None
    run_updater(arg_dir)
