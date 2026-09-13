<?php 
$require_admin = true;
require_once 'auth.php';
require_once 'db.php';

$fecha = $_GET['fecha'] ?? date('Y-m-d');
if (!is_string($fecha) || !preg_match('/^\d{4}-\d{2}-\d{2}$/', $fecha) || !strtotime($fecha)) {
    $fecha = date('Y-m-d');
}

$curso = trim($_GET['curso'] ?? '');

// Obtener cursos distintos para el filtro
$stmt = $pdo->query("SELECT DISTINCT curso FROM alumnos");
$cursos = $stmt->fetchAll(PDO::FETCH_COLUMN);

// Obtener asistencias colegio
$query = "SELECT ac.*, a.nombre, a.curso, a.numero_unico FROM asistencia_colegio ac JOIN alumnos a ON ac.alumno_id = a.numero_unico WHERE ac.fecha = ?";
$params = [$fecha];
if ($curso) {
    $query .= " AND a.curso = ?";
    $params[] = $curso;
}
$stmt = $pdo->prepare($query);
$stmt->execute($params);
$asistencias = $stmt->fetchAll(PDO::FETCH_ASSOC);

// Obtener horas comedor map
$stmt = $pdo->prepare("SELECT alumno_id, hora_ingreso FROM asistencia_comedor WHERE fecha = ?");
$stmt->execute([$fecha]);
$comedor_map = [];
while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
    $comedor_map[$row['alumno_id']] = substr($row['hora_ingreso'], 0, 5); // Quitar segundos
}

include 'header.php'; 
?>
<div class="row mb-4">
    <div class="col-md-4"><h2>Reportes</h2></div>
    <div class="col-md-8">
        <form method="get" class="row g-2 align-items-center justify-content-end">
            <div class="col-auto">
                <input type="date" class="form-control" name="fecha" value="<?php echo htmlspecialchars($fecha); ?>">
            </div>
            <div class="col-auto">
                <select name="curso" class="form-select">
                    <option value="">Todos los Cursos</option>
                    <?php foreach($cursos as $c): ?>
                    <option value="<?php echo htmlspecialchars($c); ?>" <?php if($curso === $c) echo 'selected'; ?>><?php echo htmlspecialchars($c); ?></option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-auto"><button type="submit" class="btn btn-primary">Filtrar</button></div>
            <div class="col-auto"><button type="button" class="btn btn-success" onclick="descargarPDF()">Descargar PDF</button></div>
        </form>
    </div>
</div>
<div class="row">
    <div class="col-12" id="tablaReporte">
        <div id="tituloPdf" class="d-none text-center mb-4">
            <h2>Reporte General de Asistencia</h2>
            <h4>Fecha: <?php echo date('d-m-Y', strtotime($fecha)); ?></h4>
            <hr>
        </div>
        <table class="table table-striped table-bordered">
            <thead class="table-dark">
                <tr><th>RUT/ID</th><th>Nombre</th><th>Curso</th><th>Ingreso (Colegio)</th><th>Salida (Colegio)</th><th>Ingreso (Comedor)</th></tr>
            </thead>
            <tbody>
                <?php foreach($asistencias as $a): ?>
                <tr>
                    <td><?php echo htmlspecialchars($a['numero_unico']); ?></td>
                    <td><?php echo htmlspecialchars($a['nombre']); ?></td>
                    <td><?php echo htmlspecialchars($a['curso']); ?></td>
                    <td><?php echo htmlspecialchars($a['hora_ingreso']); ?></td>
                    <td><?php echo htmlspecialchars($a['hora_salida'] ?? '-'); ?></td>
                    <td><?php echo htmlspecialchars($comedor_map[$a['numero_unico']] ?? '-'); ?></td>
                </tr>
                <?php endforeach; ?>
                <?php if(empty($asistencias)): ?>
                <tr><td colspan="6" class="text-center">No hay registros</td></tr>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Incluir html2pdf -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js"></script>
<script>
function descargarPDF() {
    const element = document.getElementById('tablaReporte');
    const tituloPdf = document.getElementById('tituloPdf');
    
    // Mostrar el título sólo para el PDF
    tituloPdf.classList.remove('d-none');
    
    const opt = {
      margin:       0.5,
      filename:     'Reporte_Asistencia_<?php echo htmlspecialchars($fecha); ?>.pdf',
      image:        { type: 'jpeg', quality: 0.98 },
      html2canvas:  { scale: 2 },
      jsPDF:        { unit: 'in', format: 'letter', orientation: 'landscape' }
    };
    
    html2pdf().set(opt).from(element).save().then(() => {
        // Volver a ocultar el título en la web
        tituloPdf.classList.add('d-none');
    });
}
</script>
<?php include 'footer.php'; ?>
