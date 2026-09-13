<?php
// API de Sincronización Asistencia v2
header("Content-Type: application/json; charset=UTF-8");

// Configuración de Base de Datos
$host = "localhost";
$db = "liceotpg_asisv2";
$user = "liceotpg_asisv2";
$pass = 'Dark19$$78';

// Clave secreta para que nadie más pueda usar tu API
$api_key = "ASISTENCIA_SECRET_2026";

// Configuración SMTP Interna (Oculta del ejecutable cliente)
$smtp_from_email = "no-reply@liceotpggm.cl";
$smtp_from_name = "Control de Asistencia";

// Verificar seguridad (soporte para diversos entornos web)
$auth = '';
if (function_exists('apache_request_headers')) {
    $headers = apache_request_headers();
    if (isset($headers['Authorization'])) $auth = $headers['Authorization'];
    elseif (isset($headers['authorization'])) $auth = $headers['authorization'];
}
if (empty($auth) && isset($_SERVER['HTTP_AUTHORIZATION'])) {
    $auth = $_SERVER['HTTP_AUTHORIZATION'];
}

if ($auth !== "Bearer " . $api_key) {
    http_response_code(401);
    echo json_encode(["error" => "No autorizado"]);
    exit;
}

// Conectar a MySQL
$conn = new mysqli($host, $user, $pass, $db);
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(["error" => "Error de conexión: " . $conn->connect_error]);
    exit;
}
$conn->set_charset("utf8mb4");

// Leer los datos JSON que envía el programa C#
$input = json_decode(file_get_contents('php://input'), true);
if (!$input || !isset($input['action'])) {
    http_response_code(400);
    echo json_encode(["error" => "Petición inválida"]);
    exit;
}

$action = $input['action'];

if ($action === 'init_db') {
    $queries = [
        "CREATE TABLE IF NOT EXISTS Alumnos (NumeroUnico VARCHAR(20) PRIMARY KEY, Nombre VARCHAR(100) NOT NULL, Curso VARCHAR(50) NOT NULL, EmailApoderado VARCHAR(100))",
        "CREATE TABLE IF NOT EXISTS AsistenciaColegio (AlumnoId VARCHAR(20), Fecha DATE, HoraIngreso TIME NOT NULL, HoraSalida TIME, PRIMARY KEY (AlumnoId, Fecha))",
        "CREATE TABLE IF NOT EXISTS AsistenciaComedor (AlumnoId VARCHAR(20), Fecha DATE, HoraIngreso TIME NOT NULL, PRIMARY KEY (AlumnoId, Fecha))",
        "CREATE TABLE IF NOT EXISTS Usuarios (Username VARCHAR(50) PRIMARY KEY, PasswordHash VARCHAR(255) NOT NULL, Role VARCHAR(50) NOT NULL, Email VARCHAR(100))"
    ];
    foreach ($queries as $q) { $conn->query($q); }
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'sync_alumnos') {
    $data = $input['data'];
    $stmt = $conn->prepare("INSERT INTO Alumnos (NumeroUnico, Nombre, Curso, EmailApoderado) VALUES (?, ?, ?, ?) ON DUPLICATE KEY UPDATE Nombre=VALUES(Nombre), Curso=VALUES(Curso), EmailApoderado=VALUES(EmailApoderado)");
    foreach ($data as $row) {
        $stmt->bind_param("ssss", $row['NumeroUnico'], $row['Nombre'], $row['Curso'], $row['EmailApoderado']);
        $stmt->execute();
    }
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'sync_usuarios') {
    $data = $input['data'];
    $stmt = $conn->prepare("INSERT INTO Usuarios (Username, PasswordHash, Role, Email) VALUES (?, ?, ?, ?) ON DUPLICATE KEY UPDATE PasswordHash=VALUES(PasswordHash), Role=VALUES(Role), Email=VALUES(Email)");
    foreach ($data as $row) {
        $stmt->bind_param("ssss", $row['Username'], $row['PasswordHash'], $row['Role'], $row['Email']);
        $stmt->execute();
    }
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'sync_asistencia_colegio') {
    $data = $input['data'];
    $stmt = $conn->prepare("INSERT INTO AsistenciaColegio (AlumnoId, Fecha, HoraIngreso, HoraSalida) VALUES (?, ?, ?, ?) ON DUPLICATE KEY UPDATE HoraIngreso=VALUES(HoraIngreso), HoraSalida=VALUES(HoraSalida)");
    foreach ($data as $row) {
        $hs = empty($row['HoraSalida']) ? null : $row['HoraSalida'];
        $hi = empty($row['HoraIngreso']) ? '' : $row['HoraIngreso'];
        $stmt->bind_param("ssss", $row['AlumnoId'], $row['Fecha'], $hi, $hs);
        $stmt->execute();
    }
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'sync_asistencia_comedor') {
    $data = $input['data'];
    $stmt = $conn->prepare("INSERT INTO AsistenciaComedor (AlumnoId, Fecha, HoraIngreso) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE HoraIngreso=VALUES(HoraIngreso)");
    foreach ($data as $row) {
        $stmt->bind_param("sss", $row['AlumnoId'], $row['Fecha'], $row['HoraIngreso']);
        $stmt->execute();
    }
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'pull_all') {
    $response = [
        "alumnos" => [],
        "usuarios" => [],
        "colegio" => [],
        "comedor" => []
    ];

    $res = $conn->query("SELECT * FROM Alumnos");
    while ($row = $res->fetch_assoc()) { $response["alumnos"][] = $row; }

    $res = $conn->query("SELECT Username, Role, Email, PasswordHash FROM Usuarios");
    while ($row = $res->fetch_assoc()) { $response["usuarios"][] = $row; }

    $res = $conn->query("SELECT * FROM AsistenciaColegio WHERE Fecha >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)");
    while ($row = $res->fetch_assoc()) { $response["colegio"][] = $row; }

    $res = $conn->query("SELECT * FROM AsistenciaComedor WHERE Fecha >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)");
    while ($row = $res->fetch_assoc()) { $response["comedor"][] = $row; }

    echo json_encode(["status" => "ok", "data" => $response]);
}
elseif ($action === 'delete_asistencia_colegio') {
    $data = $input['data'];
    $stmt = $conn->prepare("DELETE FROM AsistenciaColegio WHERE AlumnoId = ? AND Fecha = ?");
    $stmt->bind_param("ss", $data['rut'], $data['fecha']);
    $stmt->execute();
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'delete_asistencia_comedor') {
    $data = $input['data'];
    $stmt = $conn->prepare("DELETE FROM AsistenciaComedor WHERE AlumnoId = ? AND Fecha = ?");
    $stmt->bind_param("ss", $data['rut'], $data['fecha']);
    $stmt->execute();
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'delete_todo_fecha') {
    $fecha = $input['data']['fecha'];
    $stmt1 = $conn->prepare("DELETE FROM AsistenciaColegio WHERE Fecha = ?");
    $stmt1->bind_param("s", $fecha);
    $stmt1->execute();
    
    $stmt2 = $conn->prepare("DELETE FROM AsistenciaComedor WHERE Fecha = ?");
    $stmt2->bind_param("s", $fecha);
    $stmt2->execute();
    
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'delete_alumno') {
    $rut = $input['data']['rut'];
    $stmt1 = $conn->prepare("DELETE FROM AsistenciaColegio WHERE AlumnoId = ?");
    $stmt1->bind_param("s", $rut);
    $stmt1->execute();
    
    $stmt2 = $conn->prepare("DELETE FROM AsistenciaComedor WHERE AlumnoId = ?");
    $stmt2->bind_param("s", $rut);
    $stmt2->execute();
    
    $stmt3 = $conn->prepare("DELETE FROM Alumnos WHERE NumeroUnico = ?");
    $stmt3->bind_param("s", $rut);
    $stmt3->execute();
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'delete_usuario') {
    $username = $input['data']['username'];
    $stmt = $conn->prepare("DELETE FROM Usuarios WHERE Username = ?");
    $stmt->bind_param("s", $username);
    $stmt->execute();
    echo json_encode(["status" => "ok"]);
}
elseif ($action === 'send_email') {
    $to = $input['data']['to'];
    $subject = $input['data']['subject'];
    $body = $input['data']['body'];
    
    $headers = [
        'MIME-Version: 1.0',
        'Content-type: text/html; charset=UTF-8',
        'From: ' . $smtp_from_name . ' <' . $smtp_from_email . '>',
        'Reply-To: ' . $smtp_from_email,
        'X-Mailer: PHP/' . phpversion()
    ];
    
    // Si la data trae un PDF en Base64
    if (isset($input['data']['pdf_base64']) && isset($input['data']['pdf_name'])) {
        $boundary = md5(time());
        $headers[1] = "Content-Type: multipart/mixed; boundary=\"$boundary\"";
        
        $message = "--$boundary\r\n";
        $message .= "Content-Type: text/html; charset=UTF-8\r\n";
        $message .= "Content-Transfer-Encoding: 7bit\r\n\r\n";
        $message .= $body . "\r\n\r\n";
        
        $message .= "--$boundary\r\n";
        $message .= "Content-Type: application/pdf; name=\"" . $input['data']['pdf_name'] . "\"\r\n";
        $message .= "Content-Transfer-Encoding: base64\r\n";
        $message .= "Content-Disposition: attachment; filename=\"" . $input['data']['pdf_name'] . "\"\r\n\r\n";
        $message .= chunk_split($input['data']['pdf_base64']) . "\r\n";
        $message .= "--$boundary--";
        
        $body = $message;
    }

    $success = mail($to, $subject, $body, implode("\r\n", $headers));
    if ($success) {
        echo json_encode(["status" => "ok"]);
    } else {
        http_response_code(500);
        echo json_encode(["error" => "Error al enviar correo por función mail()"]);
    }
}
else {
    http_response_code(400);
    echo json_encode(["error" => "Acción desconocida"]);
}

$conn->close();
?>
