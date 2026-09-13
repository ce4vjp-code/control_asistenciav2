f = r'AsistenciaDesktop_v2\Dialogs\AgregarUsuarioWindow.xaml'
with open(f, 'rb') as file:
    content = file.read()
idx = content.find(b'Contrase')
print(content[idx:idx+20])
idx2 = content.find(b'Gesti')
print(content[idx2:idx2+20])
