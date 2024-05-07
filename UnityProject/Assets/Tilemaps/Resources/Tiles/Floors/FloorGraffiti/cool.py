import os
import re

root_dir = r"""J:\SuperFast Programs\ss13 development\unitystation\UnityProject\Assets\Tilemaps\Resources\Tiles\Floors\AsteroidFloors"""

for root, dirs, files in os.walk(root_dir):
    for file in files:
        match = re.match(r'(.*)\.([^.]+)$', file)
        if match:
            name, ext = match.groups()
            if not name.endswith('F'):
                new_file = f"{name}F.{ext}"
                os.rename(os.path.join(root, file), os.path.join(root, new_file))


