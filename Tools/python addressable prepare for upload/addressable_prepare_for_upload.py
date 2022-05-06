import sys 
import os
import json
import shutil
from pathlib import Path

SCRIPT_FOLDER_PATH = Path(__file__).parent.resolve()
REPO_PATH = SCRIPT_FOLDER_PATH.parent.parent.resolve()
ADD_PROJECT_PATH = REPO_PATH / "UnityProject" / "AddressablePackingProjects"
RESULT_CONTENT_PATH = REPO_PATH / "AddressableContent"
GITHUB_URL_TEMPLATE = "https://raw.githubusercontent.com/unitystation/unitystation/AddressableContent/{}/{}"
GITHUB_CONTENT_URL = "https://raw.githubusercontent.com/unitystation/unitystation/AddressableContent"

def main():
    # do_user_confirmation()
    print(f"Addressable project path is {ADD_PROJECT_PATH}")

    clean_content_folder(RESULT_CONTENT_PATH)
    for bundle in os.listdir(ADD_PROJECT_PATH):
        bundle_path = ADD_PROJECT_PATH / bundle
        create_bundle_content(bundle, bundle_path, RESULT_CONTENT_PATH / bundle)
    finishing_tasks()

def do_user_confirmation():
    print(
        "Have you done an addressable bundle build in the editor to ensure its up-to-date? and deleted the old bundle build?")
    user_input: str = input("y or n \n>> ")

    if user_input.lower() != "y":
        print("either you entered something other than 'y' or you haven't done it yet so do it!")
        print("...")
        sys.exit()
    else:
        print("good")

def clean_content_folder(addressable_content_path):
    if addressable_content_path.is_dir():
        print("AddressableContent folder found, nuking it...")
        shutil.rmtree(addressable_content_path)
    else:
        print("No AddressableContent folder found, creating one...")
    os.mkdir(addressable_content_path)

def create_bundle_content(bundle_name: str, original_path: Path, destination_path: Path):
    print(f"Creating bundle folder for {bundle_name}...")
    destination_path.mkdir()
    for file in os.listdir(original_path / "ServerData"):
        print(f"Copying {file}")
        shutil.copy(original_path / "ServerData" / file, destination_path)

    catalog_file = None
    for file in os.listdir(destination_path):
        if file.endswith(".json"):
            catalog_file = file

    if not catalog_file:
        print(f"No catalog file found for {bundle_name}!")
        sys.exit()

    print(f"Creating txt file for {bundle_name}...")
    with open(destination_path / f"{bundle_name}.txt", "w") as f:

        file_content = GITHUB_URL_TEMPLATE.format(bundle_name, catalog_file)
        f.write(file_content)

    correct_json_file(destination_path / catalog_file, bundle_name)

def correct_json_file(file_path: Path, bundle_name: str):
    print(f"Correcting {file_path}...")
    
    with open(file_path, "r") as f:
        json_data = json.load(f)

    with open(file_path, "w") as f:
        i = 0
        for entry in json_data["m_InternalIds"]:
            if "AddressablePackingProjects" in entry:
                bundle_file = Path(entry).name
                json_data["m_InternalIds"][i] = f"{GITHUB_CONTENT_URL}/{bundle_name}/{bundle_file}"
            i += 1
        json.dump(json_data, f)

def finishing_tasks():
    bundles =  os.listdir(RESULT_CONTENT_PATH)
    print("=========================================================")
    print("The following bundle folders were created:")
    for bundle in bundles:
        print(f"- {bundle}")
    print("=========================================================")
    print("The following urls for txt files were created:")
    for bundle in bundles:
        print(f"- {GITHUB_CONTENT_URL}/{bundle}/{bundle}.txt")
    print("=========================================================")
    print("Now commit changes and do a PR. Good bye!")

if __name__ == "__main__":
    main()