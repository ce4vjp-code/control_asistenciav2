<?php
require_once 'auth.php';
require_once 'db.php';

$msg = '';
$error = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['cambiar_clave'])) {
    if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
        $error = "Token de seguridad inválido. Recargue la página.";
    } else {
        $clave_actual = $_POST['clave_actual'] ?? '';
        $nueva_clave = $_POST['nueva_clave'] ?? '';
        $confirmar_clave = $_POST['confirmar_clave'] ?? '';
        
        $username = $_SESSION['admin_username'] ?? '';
        
        $stmt = $pdo->prepare("SELECT password FROM usuarios WHERE username = ?");
        $stmt->execute([$username]);
        $user = $stmt->fetch(PDO::FETCH_ASSOC);
        
        if (!$user || !password_verify($clave_actual, $user['password'])) {
            $error = "La contraseña actual es incorrecta.";
        } elseif ($nueva_clave !== $confirmar_clave) {
            $error = "Las contraseñas nuevas no coinciden.";
        } elseif (strlen($nueva_clave) < 8) {
            $error = "La nueva contraseña debe tener al menos 8 caracteres por seguridad.";
        } else {
            $hash = password_hash($nueva_clave, PASSWORD_DEFAULT);
            $stmt = $pdo->prepare("UPDATE usuarios SET password = ? WHERE username = ?");
            $stmt->execute([$hash, $username]);
            require_once 'audit.php';
            registrar_bitacora($pdo, 'CAMBIO_CLAVE', "Usuario: {$username}");
            $msg = "Contraseña actualizada exitosamente.";
        }
    }
}

include 'header.php';
?>
<div class="row">
    <div class="col-md-6 offset-md-3 mt-5">
        <div class="card shadow">
            <div class="card-header bg-primary text-white">
                <h5 class="mb-0">Cambiar Contraseña</h5>
            </div>
            <div class="card-body">
                <?php if($error): ?><div class="alert alert-danger"><?php echo htmlspecialchars($error); ?></div><?php endif; ?>
                <?php if($msg): ?><div class="alert alert-success"><?php echo htmlspecialchars($msg); ?></div><?php endif; ?>
                
                <form method="post" action="cambiar_clave.php">
                    <?php csrf_field(); ?>
                    <input type="hidden" name="cambiar_clave" value="1">
                    <div class="mb-3">
                        <label class="form-label">Contraseña Actual</label>
                        <input type="password" name="clave_actual" class="form-control" required autofocus>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Nueva Contraseña</label>
                        <input type="password" name="nueva_clave" class="form-control" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Confirmar Nueva Contraseña</label>
                        <input type="password" name="confirmar_clave" class="form-control" required>
                    </div>
                    <button type="submit" class="btn btn-success w-100">Actualizar Contraseña</button>
                </form>
            </div>
        </div>
    </div>
</div>
<?php include 'footer.php'; ?>
