import os
import shutil
import json
import argparse

def get_files_to_keep_in_managed():
    path = os.path.abspath(os.path.join(
        os.path.dirname(__file__),
        "../CodeScanning/CodeScan/CodeScan/bin/Debug/net7.0/FilesToMoveToManaged.json"
    ))
    with open(path, 'r') as file:
        return json.load(file)

files_to_keep_in_managed = get_files_to_keep_in_managed()

def prepare_windows(path):
    global files_to_keep_in_managed
    
    managed_dir = os.path.join(path, "Unitystation_Data", "Managed")
    for file in os.listdir(managed_dir):
        file_path = os.path.join(managed_dir, file)
        if os.path.isfile(file_path):
            file_name, _ = os.path.splitext(file)
            if file_name not in files_to_keep_in_managed:
                os.remove(file_path)

    resources_dir = os.path.join(path, "Unitystation_Data", "Resources")
    shutil.rmtree(resources_dir, ignore_errors=True)

    streaming_assets_dir = os.path.join(path, "Unitystation_Data", "StreamingAssets")
    shutil.rmtree(streaming_assets_dir, ignore_errors=True)

    data_dir = os.path.join(path, "Unitystation_Data")
    for file in os.listdir(data_dir):
        file_path = os.path.join(data_dir, file)
        if os.path.isfile(file_path):
            os.remove(file_path)

def prepare_linux(path):
    global files_to_keep_in_managed
    
    managed_dir = os.path.join(path, "Unitystation_Data", "Managed")
    for file in os.listdir(managed_dir):
        file_path = os.path.join(managed_dir, file)
        if os.path.isfile(file_path):
            file_name, _ = os.path.splitext(file)
            if file_name not in files_to_keep_in_managed:
                os.remove(file_path)

    resources_dir = os.path.join(path, "Unitystation_Data", "Resources")
    shutil.rmtree(resources_dir, ignore_errors=True)

    streaming_assets_dir = os.path.join(path, "Unitystation_Data", "StreamingAssets")
    shutil.rmtree(streaming_assets_dir, ignore_errors=True)

    data_dir = os.path.join(path, "Unitystation_Data")
    for file in os.listdir(data_dir):
        file_path = os.path.join(data_dir, file)
        if os.path.isfile(file_path):
            os.remove(file_path)

def prepare_mac(path):
    global files_to_keep_in_managed
    
    managed_dir = os.path.join(path, "Unitystation.app", "Contents", "Resources", "Data", "Managed")
    for file in os.listdir(managed_dir):
        file_path = os.path.join(managed_dir, file)
        if os.path.isfile(file_path):
            file_name, _ = os.path.splitext(file)
            if file_name not in files_to_keep_in_managed:
                os.remove(file_path)

    streaming_assets_dir = os.path.join(path, "Unitystation.app", "Contents", "Resources", "Data", "StreamingAssets")
    shutil.rmtree(streaming_assets_dir, ignore_errors=True)

    data_dir = os.path.join(path, "Unitystation.app", "Contents", "Resources", "Data")
    for file in os.listdir(data_dir):
        file_path = os.path.join(data_dir, file)
        if os.path.isfile(file_path):
            os.remove(file_path)


parser = argparse.ArgumentParser(description="Prepare Unitystation builds for different platforms.")
parser.add_argument("--windows", type=str, help="Path to the Windows build directory")
parser.add_argument("--linux", type=str, help="Path to the Linux build directory")
parser.add_argument("--mac", type=str, help="Path to the Mac build directory")
args = parser.parse_args()


# Process builds based on specified paths
if args.windows:
    print(f"Preparing Windows build at {args.windows}...")
    prepare_windows(args.windows)

if args.linux:
    print(f"Preparing Linux build at {args.linux}...")
    prepare_linux(args.linux)

if args.mac:
    print(f"Preparing Mac build at {args.mac}...")
    prepare_mac(args.mac)

