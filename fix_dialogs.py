import os

files = [
    r'AsistenciaDesktop_v2\Dialogs\AgregarUsuarioWindow.xaml',
    r'AsistenciaDesktop_v2\Dialogs\EditarUsuarioWindow.xaml',
    r'AsistenciaDesktop_v2\Dialogs\AgregarUsuarioWindow.xaml.cs',
    r'AsistenciaDesktop_v2\Dialogs\EditarUsuarioWindow.xaml.cs'
]

replacements = {
    b'Gesti\xef\xbf\xbdn de Usuario': b'Gesti\xc3\xb3n de Usuario',
    b'Contrase\xef\xbf\xbd': b'Contrase\xc3\xb1a',
    b'Recepci\xef\xbf\xbdn': b'Recepci\xc3\xb3n',
    b'\xef\xbf\xbd%xito': b'\xc3\x89xito',
    b'Error Cr\xef\xbf\xbd': b'Error Cr\xc3\xadtico'
}

for f in files:
    with open(f, 'rb') as file:
        content = file.read()
    
    for old, new in replacements.items():
        content = content.replace(old, new)
        
    with open(f, 'wb') as file:
        file.write(content)
