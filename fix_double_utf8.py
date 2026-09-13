import os
import glob

files = glob.glob(r'AsistenciaDesktop_v2\Dialogs\*.xaml*')
for f in files:
    with open(f, 'rb') as file:
        content = file.read()
    
    # fix double utf-8
    content = content.replace(b'\xc3\x83\xc2\xb3', b'\xc3\xb3') # ó
    content = content.replace(b'\xc3\x83\xc2\xb1', b'\xc3\xb1') # ñ
    content = content.replace(b'\xc3\x83\xc2\x89', b'\xc3\x89') # É
    content = content.replace(b'\xc3\x83\xc2\xad', b'\xc3\xad') # í
    content = content.replace(b'\xc3\x83\xc2\xbf', b'\xc2\xbf') # ¿
    
    # check for replacement characters just in case
    content = content.replace(b'Contrase\xef\xbf\xbd', b'Contrase\xc3\xb1a')
    content = content.replace(b'Gesti\xef\xbf\xbdn', b'Gesti\xc3\xb3n')
    
    with open(f, 'wb') as file:
        file.write(content)
