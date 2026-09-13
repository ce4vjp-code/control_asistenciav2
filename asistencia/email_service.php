<?php
// Si descargaste PHPMailer manualmente, asegúrate de incluir los archivos:
use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

$phpmailer_path = __DIR__ . '/PHPMailer/src/';
if (file_exists($phpmailer_path . 'PHPMailer.php')) {
    require_once $phpmailer_path . 'Exception.php';
    require_once $phpmailer_path . 'PHPMailer.php';
    require_once $phpmailer_path . 'SMTP.php';
} elseif (file_exists(__DIR__ . '/vendor/autoload.php')) {
    require_once __DIR__ . '/vendor/autoload.php';
}

function enviar_correo($to, $subject, $body_content) {
    if (!class_exists('PHPMailer\PHPMailer\PHPMailer')) {
        error_log("PHPMailer no está instalado o no se encuentra en el directorio PHPMailer/");
        return false;
    }

    $config = require __DIR__ . '/config.php';
    $mailConfig = $config['mail'];

    $mail = new PHPMailer(true);

    try {
        $mail->isSMTP();
        $mail->Host       = $mailConfig['host'];
        $mail->SMTPAuth   = true;
        $mail->Username   = $mailConfig['username'];
        $mail->Password   = $mailConfig['password'];
        $mail->SMTPSecure = PHPMailer::ENCRYPTION_SMTPS;
        $mail->Port       = $mailConfig['port'];
        $mail->CharSet    = 'UTF-8';

        $mail->setFrom($mailConfig['username'], $mailConfig['from_name']);
        $mail->addAddress($to);

        $html = '
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="UTF-8">
            <style>
                body { font-family: Arial, sans-serif; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }
                .container { max-width: 600px; margin: 20px auto; background-color: #fff; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }
                .header { background-color: #002366; color: #fff; text-align: center; padding: 25px 20px; }
                .header h1 { margin: 0; font-size: 22px; font-weight: normal; letter-spacing: 1px; }
                .content { padding: 30px; line-height: 1.6; font-size: 16px; }
                .footer { background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 13px; color: #666; border-top: 1px solid #eee; }
                .highlight { font-size: 18px; font-weight: bold; color: #002366; }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>' . htmlspecialchars($mailConfig['from_name'], ENT_QUOTES, 'UTF-8') . '</h1>
                </div>
                <div class="content">
                    ' . $body_content . '
                </div>
                <div class="footer">
                    <p><strong>' . htmlspecialchars($mailConfig['from_name'], ENT_QUOTES, 'UTF-8') . '</strong></p>
                    <p>Ignacio Carrera Pinto 369, Estación Yumbel</p>
                    <p>Teléfono: +56 9 7799 5867</p>
                </div>
            </div>
        </body>
        </html>
        ';

        $mail->isHTML(true);
        $mail->Subject = $subject;
        $mail->Body    = $html;

        return $mail->send();
    } catch (Exception $e) {
        error_log("No se pudo enviar el correo a $to. Error: {$mail->ErrorInfo}");
        return false;
    }
}

function notificar_ingreso($email, $nombre, $hora) {
    if (!$email) return;
    $nombre_safe = htmlspecialchars($nombre, ENT_QUOTES, 'UTF-8');
    $hora_safe = htmlspecialchars($hora, ENT_QUOTES, 'UTF-8');

    $body = "<p>Estimado Apoderado,</p><p>Le informamos que el estudiante <strong>{$nombre_safe}</strong> ha registrado su <span class='highlight'>ingreso</span> al establecimiento.</p><p><strong>Hora:</strong> {$hora_safe}</p><br><p>Atentamente,<br>Inspectoría General.</p>";
    enviar_correo($email, "Ingreso Registrado - " . $nombre, $body);
}

function notificar_salida($email, $nombre, $hora) {
    if (!$email) return;
    $nombre_safe = htmlspecialchars($nombre, ENT_QUOTES, 'UTF-8');
    $hora_safe = htmlspecialchars($hora, ENT_QUOTES, 'UTF-8');

    $body = "<p>Estimado Apoderado,</p><p>Le informamos que el estudiante <strong>{$nombre_safe}</strong> ha registrado su <span class='highlight'>salida</span> del establecimiento.</p><p><strong>Hora:</strong> {$hora_safe}</p><br><p>Atentamente,<br>Inspectoría General.</p>";
    enviar_correo($email, "Salida Registrada - " . $nombre, $body);
}

function notificar_alerta_comedor($alumnos) {
    if (empty($alumnos)) return;
    
    $config = require __DIR__ . '/config.php';
    $admin = $config['mail']['admin_alert'];
    
    $items = "";
    foreach($alumnos as $a) {
        $nom_safe = htmlspecialchars($a['nombre'], ENT_QUOTES, 'UTF-8');
        $cur_safe = htmlspecialchars($a['curso'], ENT_QUOTES, 'UTF-8');
        $items .= "<li><strong>{$nom_safe}</strong> (Curso: {$cur_safe})</li>";
    }
    $body = "<p>Los siguientes alumnos ingresaron al colegio hoy, pero <strong style='color:red;'>no han registrado asistencia en el comedor:</strong></p><ul>$items</ul>";
    enviar_correo($admin, "Alerta Comedor - Alumnos Faltantes", $body);
}
?>
