import os

files = [
    r'AsistenciaDesktop_v2\Views\LoginWindow.xaml',
    r'AsistenciaDesktop_v2\Views\AlumnosView.xaml',
    r'AsistenciaDesktop_v2\Views\UsuariosView.xaml',
    r'AsistenciaDesktop_v2\Views\MainShell.xaml'
]

for f in files:
    if os.path.exists(f):
        with open(f, 'rb') as file:
            content = file.read()
        
        # Replace the ASCII question mark 3F or whatever it is, or we decode with replace
        # Actually, let's just decode using 'latin1' because that's what PowerShell Get-Content -Raw did (ANSI)
        # and then write back as UTF-8
        
        # If it has a BOM, we can just read it normally if it's correct.
        
        # Let's just fix the strings we know are broken
        # It could be we wrote 'Gesti?n' as literal b'Gesti?n'
        
        content = content.replace(b'INICIAR SESIN', 'INICIAR SESIÓN'.encode('utf-8'))
        content = content.replace(b'INICIAR SESI?N', 'INICIAR SESIÓN'.encode('utf-8'))
        content = content.replace(b'Contrasea', 'Contraseña'.encode('utf-8'))
        content = content.replace(b'Contrase?a', 'Contraseña'.encode('utf-8'))
        content = content.replace(b'Gestin de', 'Gestión de'.encode('utf-8'))
        content = content.replace(b'Gesti?n de', 'Gestión de'.encode('utf-8'))
        content = content.replace(b'Men? principal', 'Menú principal'.encode('utf-8'))
        
        with open(f, 'wb') as file:
            file.write(content)
