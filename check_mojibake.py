import os
import glob

files = glob.glob(r'AsistenciaDesktop_v2\Dialogs\*.xaml*')
files += glob.glob(r'AsistenciaDesktop_v2\Views\*.xaml*')
for f in files:
    with open(f, 'rb') as file:
        content = file.read()
    if b'\xef\xbf\xbd' in content:
        print("Found in: " + f)
