import json
import os
import xml.etree.ElementTree as ET
import zipfile
import shutil
import tempfile

# C# project file
CSPROJ_PATH = "MoreUI.csproj"

if __name__ == "__main__":
    # Load the c# project file to pull information from
    csproj_et = ET.parse(CSPROJ_PATH)

    # Get assembly name
    assembly_name = csproj_et.findtext("./PropertyGroup/AssemblyName")
    if assembly_name is None:
        raise Exception("Could not find assembly name in csproj file")

    # Get project name
    project_name = csproj_et.findtext("./PropertyGroup/Product")
    if project_name is None:
        project_name = assembly_name

    # Get version
    version = csproj_et.findtext("./PropertyGroup/Version")
    if version is None:
        raise Exception("Could not find AssemblyVersion node in csproj file")

    # Get description
    description = csproj_et.findtext("./PropertyGroup/Description")
    if description is None:
        description = ""

    # Generate the manifest.json file
    with open("manifest.json", "r") as f:
        manifest = json.load(f)

    manifest["name"] = project_name
    manifest["version_number"] = version
    manifest["description"] = description

    manifest_text = json.dumps(manifest, indent=4)

    # Clear output folder
    with tempfile.TemporaryDirectory() as tmpdir:
        # Copy everything from the assets folder to the output directory
        shutil.copytree("assets", os.path.join(tmpdir, "assets"))

        # Copy over the output directory
        os.makedirs(os.path.join(tmpdir, "BepInEx", "plugins", assembly_name))
        shutil.copytree("out", os.path.join(tmpdir, "BepInEx", "plugins", assembly_name), dirs_exist_ok=True)

        # Write manifest.json to temp folder
        with open(os.path.join(tmpdir, "manifest.json"), "w") as f:
            f.write(manifest_text)

        # Copy over the README and icon
        shutil.copy("README.md", tmpdir)
        shutil.copy("icon.png", tmpdir)

        # Zip the temp folder
        with zipfile.ZipFile(project_name + ".zip", "w") as zip:
            for root, dirs, files in os.walk(tmpdir):
                for file in files:
                    zip.write(os.path.join(root, file), os.path.relpath(os.path.join(root, file), tmpdir))
