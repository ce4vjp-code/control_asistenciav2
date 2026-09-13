import os

f = r'AsistenciaDesktop_v2\Views\UsuariosView.xaml'
with open(f, 'rb') as file:
    content = file.read()

content = content.replace(b'Gesti\xef\xbf\xbdn de Usuarios', b'Gesti\xc3\xb3n de Usuarios')
content = content.replace(b'Configuraci\xef\xbf\xbdn de Tareas', b'Configuraci\xc3\xb3n de Tareas')
content = content.replace(b'Configuraci\xef\xbf\xbdn del Sistema', b'Configuraci\xc3\xb3n del Sistema')
content = content.replace(b'GUARDAR CONFIGURACI\xef\xbf\xbdN', b'GUARDAR CONFIGURACI\xc3\x93N')

with open(f, 'wb') as file:
    file.write(content)
