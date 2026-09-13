<?php 
// Diagnóstico de errores visible
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL & ~E_NOTICE & ~E_DEPRECATED);

require_once __DIR__ . '/session_config.php';
require_once 'db.php';
require_once 'audit.php';

$error = '';
$info = '';

if (isset($_GET['timeout'])) {
    $info = "Su sesión ha expirado por inactividad (30 minutos). Por favor, inicie sesión nuevamente.";
}

// Almacenamiento privado para rate limiting (aislado del /tmp compartido de cPanel)
$client_ip = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
$storage_dir = __DIR__ . '/storage';
if (!is_dir($storage_dir)) {
    @mkdir($storage_dir, 0750, true);
}
$rate_file = $storage_dir . '/limit_' . md5($client_ip) . '.json';
$ip_data = ['attempts' => 0, 'lockout' => 0];

if (file_exists($rate_file)) {
    $content = @file_get_contents($rate_file);
    if ($content) {
        $decoded = json_decode($content, true);
        if (is_array($decoded)) {
            $ip_data = $decoded;
        }
    }
}

$is_locked = time() < ($ip_data['lockout'] ?? 0);

if ($is_locked) {
    $minutos_restantes = ceil(($ip_data['lockout'] - time()) / 60);
    $error = "Demasiados intentos fallidos desde su dirección IP. Por seguridad, intente de nuevo en {$minutos_restantes} minuto(s).";
} elseif ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['login'])) {
    // Validar token CSRF
    $token = $_POST['csrf_token'] ?? '';
    if (!verify_csrf_token($token)) {
        $error = "Token de seguridad inválido o sesión expirada. Recargue la página.";
    } else {
        $username = trim($_POST['username'] ?? '');
        $password = $_POST['password'] ?? '';

        $stmt = $pdo->prepare("SELECT * FROM usuarios WHERE username = ?");
        $stmt->execute([$username]);
        $user = $stmt->fetch(PDO::FETCH_ASSOC);

        if ($user && password_verify($password, $user['password'])) {
            // Mitigación de fijación de sesión (Session Fixation)
            session_regenerate_id(true);
            
            // Reiniciar intentos de la IP
            if (file_exists($rate_file)) {
                @unlink($rate_file);
            }

            $_SESSION['admin_logged_in'] = true;
            $_SESSION['admin_username'] = $user['username'];
            $_SESSION['user_rol'] = $user['rol'] ?? 'operador';
            $_SESSION['last_activity'] = time();

            registrar_bitacora($pdo, 'LOGIN_EXITOSO', "Usuario: {$user['username']}, Rol: {$_SESSION['user_rol']}");
            
            header("Location: index.php");
            exit;
        } else {
            // Retardo leve anti-timing
            sleep(1);
            
            $ip_data['attempts'] = ($ip_data['attempts'] ?? 0) + 1;
            
            if ($ip_data['attempts'] >= 5) {
                $ip_data['lockout'] = time() + (5 * 60); // 5 minutos de bloqueo
                $error = "Demasiados intentos fallidos. Su dirección IP ha sido bloqueada temporalmente por 5 minutos.";
            } else {
                $intentos_restantes = 5 - $ip_data['attempts'];
                $error = "Usuario o contraseña incorrectos. (Intentos restantes: {$intentos_restantes})";
            }
            
            registrar_bitacora($pdo, 'LOGIN_FALLIDO', "Usuario intentado: {$username}");
            @file_put_contents($rate_file, json_encode($ip_data));
        }
    }
}

$is_logged_in = isset($_SESSION['admin_logged_in']) && $_SESSION['admin_logged_in'] === true;

include 'header.php'; 
?>
<div class="row">
    <div class="col-md-12 text-center">
        <h1 class="display-4">Plataforma de Asistencia</h1>
        <hr>
        
        <?php if (!$is_logged_in): ?>
            <div class="col-md-4 offset-md-4 mt-5 text-start">
                <div class="card shadow">
                    <div class="card-header bg-primary text-white">
                        <h5 class="mb-0">Iniciar Sesión</h5>
                    </div>
                    <div class="card-body">
                        <?php if($info): ?><div class="alert alert-warning"><?php echo htmlspecialchars($info); ?></div><?php endif; ?>
                        <?php if($error): ?><div class="alert alert-danger"><?php echo htmlspecialchars($error); ?></div><?php endif; ?>
                        <form method="post" action="index.php">
                            <?php csrf_field(); ?>
                            <input type="hidden" name="login" value="1">
                            <div class="mb-3">
                                <label class="form-label">Usuario</label>
                                <input type="text" name="username" class="form-control" required autofocus autocomplete="username">
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Contraseña</label>
                                <input type="password" name="password" class="form-control" required autocomplete="current-password">
                            </div>
                            <button type="submit" class="btn btn-primary w-100" <?php echo $is_locked ? 'disabled' : ''; ?>>Ingresar</button>
                        </form>
                    </div>
                </div>
            </div>
        <?php else: ?>
            <p class="lead mt-4">Bienvenido al panel de control, <strong><?php echo htmlspecialchars($_SESSION['admin_username']); ?></strong>.</p>
            <p>Seleccione una opción del menú superior para comenzar.</p>
        <?php endif; ?>
    </div>
</div>
<?php include 'footer.php'; ?>
