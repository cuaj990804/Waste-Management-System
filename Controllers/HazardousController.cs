using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.Models;
using System.IO;

namespace SGA.Controllers
{
    public class HazardousController : Controller
    {
        private readonly SgaContext _context;
        private readonly IConfiguration _config;

        public HazardousController(SgaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        // GET: Hazardous
        public async Task<IActionResult> Index()
        {
            ViewBag.HazardousAreas = _context.HazardousAreas.Select(p => p.AreaName).ToList();
            ViewBag.HazardousWastes = _context.HazardousWastes
                .Where(w => w.IsActive)
                .Select(w => w.WasteName)
                .ToList();
            return View(await _context.HazardousWasteManifests.ToListAsync());
        }


        [HttpPost]
        public JsonResult GetTable()
        {
            try
            {
                int.TryParse(Request.Form["draw"].FirstOrDefault(), out int NroPeticion);
                int.TryParse(Request.Form["length"].FirstOrDefault(), out int CantidadRegistros);
                int.TryParse(Request.Form["start"].FirstOrDefault(), out int OmitirRegistros);

                // Obtener filtros adicionales desde los parámetros
                string folioFilter = Request.Form["Folio"].FirstOrDefault() ?? "";
                var wasteNameFilters = ParseMultiSelectFilter(Request.Form["WasteName"].FirstOrDefault());
                string generationAreaFilter = Request.Form["GenerationArea"].FirstOrDefault() ?? "";
                string manifestNumberFilter = Request.Form["ManifestNumber"].FirstOrDefault() ?? "";
                string manifestSealedFilter = Request.Form["ManifestSealed"].FirstOrDefault() ?? "";

                // Obtener rango de fechas desde los parámetros
                string startDateIntoStr = Request.Form["startDateInto"].FirstOrDefault();
                string endDateIntoStr = Request.Form["endDateInto"].FirstOrDefault();
                string startDateOutStr = Request.Form["startDateOut"].FirstOrDefault();
                string endDateOutStr = Request.Form["endDateOut"].FirstOrDefault();

                DateOnly? startDateInto = null, endDateInto = null, startDateOut = null, endDateOut = null;

                if (DateOnly.TryParse(startDateIntoStr, out DateOnly parsedStartDateInto))
                    startDateInto = parsedStartDateInto;

                if (DateOnly.TryParse(endDateIntoStr, out DateOnly parsedEndDateInto))
                    endDateInto = parsedEndDateInto;

                if (DateOnly.TryParse(startDateOutStr, out DateOnly parsedStartDateOut))
                    startDateOut = parsedStartDateOut;

                if (DateOnly.TryParse(endDateOutStr, out DateOnly parsedEndDateOut))
                    endDateOut = parsedEndDateOut;

                // Base query
                var query = _context.HazardousWasteManifests.AsQueryable();

                // Filtros adicionales
                if (!string.IsNullOrEmpty(folioFilter))
                    query = query.Where(e => e.Folio.Contains(folioFilter));

                if (wasteNameFilters.Count > 0)
                    query = query.Where(e => e.WasteName != null && wasteNameFilters.Contains(e.WasteName));

                if (!string.IsNullOrEmpty(generationAreaFilter))
                    query = query.Where(e => e.GenerationArea.Contains(generationAreaFilter));

                if (!string.IsNullOrEmpty(manifestNumberFilter))
                    query = query.Where(e => e.ManifestNumber.Contains(manifestNumberFilter));

                if (!string.IsNullOrEmpty(manifestSealedFilter))
                {
                    bool isSealed = manifestSealedFilter.ToLower() == "si" || manifestSealedFilter.ToLower() == "true";
                    query = query.Where(e => e.ManifestSealed == isSealed);
                }

                // Filtros de fechas
                if (startDateInto.HasValue)
                    query = query.Where(e => e.WarehouseEntryDate >= startDateInto);

                if (endDateInto.HasValue)
                    query = query.Where(e => e.WarehouseEntryDate <= endDateInto);

                if (startDateOut.HasValue)
                    query = query.Where(e => e.WarehouseExitDate.HasValue && e.WarehouseExitDate >= startDateOut);

                if (endDateOut.HasValue)
                    query = query.Where(e => e.WarehouseExitDate.HasValue && e.WarehouseExitDate <= endDateOut);

                // Total registros antes y después del filtro
                int TotalRegistros = _context.HazardousWasteManifests.Count();
                int TotalRegistrosFiltrados = query.Count();

                // Ordenar por fecha de entrada descendente
                query = query.OrderByDescending(e => e.WarehouseEntryDate);

                var lista = query
                    .Skip(OmitirRegistros)
                    .Take(CantidadRegistros)
                    .Select(e => new
                    {
                        e.Id,
                        e.Folio,
                        e.WasteName,
                        e.Quantity,
                        e.WeightKg,
                        e.Corrosive,
                        e.Reactive,
                        e.Explosive,
                        e.Toxic,
                        e.Flammable,
                        e.Biological,
                        e.GenerationArea,
                        e.GenerationManagerName,
                        e.WarehouseEntryDate,
                        e.WarehouseExitDate,
                        e.ManifestNumber,
                        e.ManifestDeliveredBy,
                        e.ManifestReceivedBy,
                        e.CollectionTransportName,
                        e.CollectionTransportAuthNumber,
                        e.FinalDisposalName,
                        e.FinalDisposalAuthNumber,
                        e.ManifestSealed,
                        e.Comments
                    })
                    .ToList();

                // Respuesta al cliente
                return Json(new
                {
                    draw = NroPeticion,
                    recordsTotal = TotalRegistros,
                    recordsFiltered = TotalRegistrosFiltrados,
                    data = lista
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var hazardous = await _context.HazardousWasteManifests
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hazardous == null) return NotFound();

            return PartialView(hazardous);
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                ViewBag.HazardousAreas = _context.HazardousAreas.Select(p => p.AreaName).ToList();

                if (id == null)
                {
                    return NotFound();
                }

                var hazardous = await _context.HazardousWasteManifests.FindAsync(id);
                if (hazardous == null)
                {
                    return NotFound();
                }

                return PartialView("Edit", hazardous);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HazardousWasteManifest formModel)
        {
            if (id != formModel.Id)
                return Json(new { success = false, message = "ID no coincide" });

            if (ModelState.IsValid)
            {
                var dbModel = await _context.HazardousWasteManifests.FindAsync(id);
                if (dbModel == null)
                    return Json(new { success = false, message = "Registro no encontrado" });

                // Actualizar campos
                dbModel.Folio = formModel.Folio;
                dbModel.WasteName = formModel.WasteName;
                dbModel.Quantity = formModel.Quantity;
                dbModel.WeightKg = formModel.WeightKg;
                dbModel.Corrosive = formModel.Corrosive;
                dbModel.Reactive = formModel.Reactive;
                dbModel.Explosive = formModel.Explosive;
                dbModel.Toxic = formModel.Toxic;
                dbModel.Flammable = formModel.Flammable;
                dbModel.Biological = formModel.Biological;
                dbModel.GenerationArea = formModel.GenerationArea;
                dbModel.GenerationManagerName = formModel.GenerationManagerName;
                dbModel.WarehouseEntryDate = formModel.WarehouseEntryDate;
                dbModel.WarehouseExitDate = formModel.WarehouseExitDate;
                dbModel.ManifestNumber = formModel.ManifestNumber;
                dbModel.ManifestDeliveredBy = formModel.ManifestDeliveredBy;
                dbModel.ManifestReceivedBy = formModel.ManifestReceivedBy;
                dbModel.CollectionTransportName = formModel.CollectionTransportName;
                dbModel.CollectionTransportAuthNumber = formModel.CollectionTransportAuthNumber;
                dbModel.FinalDisposalName = formModel.FinalDisposalName;
                dbModel.FinalDisposalAuthNumber = formModel.FinalDisposalAuthNumber;
                dbModel.ManifestSealed = formModel.ManifestSealed;
                dbModel.Comments = formModel.Comments;
                dbModel.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Datos inválidos" });
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                var item = _context.HazardousWasteManifests.Find(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Registro no encontrado." });
                }

                _context.HazardousWasteManifests.Remove(item);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.HazardousAreas = _context.HazardousAreas.Select(p => p.AreaName).ToList();
            ViewBag.HazardousWastes = _context.HazardousWastes
                .Where(w => w.IsActive)
                .Select(w => w.WasteName)
                .ToList();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HazardousWasteManifest model)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(model.WasteName))
                ModelState.AddModelError("WasteName", "El residuo es obligatorio.");

            if (string.IsNullOrWhiteSpace(model.GenerationArea))
                ModelState.AddModelError("GenerationArea", "El área de generación es obligatoria.");

            if (!model.Quantity.HasValue || model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "La cantidad debe ser mayor a 0.");

            if (!model.WeightKg.HasValue || model.WeightKg <= 0)
                ModelState.AddModelError("WeightKg", "El peso debe ser mayor a 0.");

            if (ModelState.IsValid)
            {
                // Generar Folio automático: FYYMMDDHHMM{Cantidad}
                var now = DateTime.Now;
                string folio = $"F{now:yyMMddHHmm}{(int)model.Quantity.Value}";

                model.Folio = folio;
                model.WarehouseEntryDate = DateOnly.FromDateTime(now);
                model.ManifestSealed = false;
                model.CreatedDate = now;

                // Asegurarse de que los valores CRETIB null se conviertan a false
                model.Corrosive = model.Corrosive ?? false;
                model.Reactive = model.Reactive ?? false;
                model.Explosive = model.Explosive ?? false;
                model.Toxic = model.Toxic ?? false;
                model.Flammable = model.Flammable ?? false;
                model.Biological = model.Biological ?? false;

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registro creado correctamente";
                return RedirectToAction(nameof(Create));
            }

            ViewBag.HazardousAreas = _context.HazardousAreas.Select(p => p.AreaName).ToList();
            ViewBag.HazardousWastes = _context.HazardousWastes
                .Where(w => w.IsActive)
                .Select(w => w.WasteName)
                .ToList();
            return View(model);
        }

        [HttpGet]
        public JsonResult GetHazardousWasteData(string wasteName)
        {
            var waste = _context.HazardousWastes
                .Include(w => w.HazardousWasteCretibs)
                    .ThenInclude(wc => wc.Cretib)
                .FirstOrDefault(w => w.WasteName == wasteName && w.IsActive);

            if (waste == null)
            {
                return Json(new { success = false });
            }

            // Obtener las claves CRETIB asociadas
            var cretibKeys = waste.HazardousWasteCretibs
                .Select(wc => wc.Cretib.CretibKey)
                .ToList();

            return Json(new
            {
                success = true,
                wasteKey = waste.WasteKey,
                wasteName = waste.WasteName,
                wasteDescription = waste.WasteDescription,
                corrosive = cretibKeys.Contains("C"),
                reactive = cretibKeys.Contains("R"),
                explosive = cretibKeys.Contains("E"),
                toxic = cretibKeys.Contains("T"),
                flammable = cretibKeys.Contains("I"),
                biological = cretibKeys.Contains("B")
            });
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        public IActionResult ExportToExcel(
            string? folio,
            string? wasteName,
            string? generationArea,
            string? manifestNumber,
            string? startDateInto,
            string? endDateInto,
            string? startDateOut,
            string? endDateOut
        )
        {
            var plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "BitcoraResiduosPeligrosos.xlsx");

            if (!System.IO.File.Exists(plantillaPath))
            {
                return NotFound("No se encontró el archivo de plantilla.");
            }

            try
            {
                // Consulta con filtros dinámicos
                IQueryable<HazardousWasteManifest> query = _context.HazardousWasteManifests;

                if (!string.IsNullOrWhiteSpace(folio))
                    query = query.Where(r => r.Folio == folio);

                var wasteNameFilters = ParseMultiSelectFilter(wasteName);
                if (wasteNameFilters.Count > 0)
                    query = query.Where(r => r.WasteName != null && wasteNameFilters.Contains(r.WasteName));

                if (!string.IsNullOrWhiteSpace(generationArea))
                    query = query.Where(r => r.GenerationArea == generationArea);

                if (!string.IsNullOrWhiteSpace(manifestNumber))
                    query = query.Where(r => r.ManifestNumber == manifestNumber);

                if (DateOnly.TryParse(startDateInto, out DateOnly startInto))
                    query = query.Where(r => r.WarehouseEntryDate >= startInto);

                if (DateOnly.TryParse(endDateInto, out DateOnly endInto))
                    query = query.Where(r => r.WarehouseEntryDate <= endInto);

                if (DateOnly.TryParse(startDateOut, out DateOnly startOut))
                    query = query.Where(r => r.WarehouseExitDate.HasValue && r.WarehouseExitDate >= startOut);

                if (DateOnly.TryParse(endDateOut, out DateOnly endOut))
                    query = query.Where(r => r.WarehouseExitDate.HasValue && r.WarehouseExitDate <= endOut);

                // Limitar la cantidad de registros
                List<HazardousWasteManifest> registros;
                try
                {
                    registros = query.Take(1000).ToList();
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Error al consultar la base de datos: {dbEx.Message}");
                    return StatusCode(500, $"Error al consultar la base de datos: {dbEx.Message}");
                }

                if (!registros.Any())
                {
                    return NoContent();
                }

                // Crear el archivo Excel
                XLWorkbook workbook;
                try
                {
                    workbook = new XLWorkbook(plantillaPath);
                }
                catch (Exception excelEx)
                {
                    Console.WriteLine($"Error al abrir la plantilla de Excel: {excelEx.Message}");
                    return StatusCode(500, $"Error al abrir la plantilla de Excel: {excelEx.Message}");
                }

                using (workbook)
                {
                    IXLWorksheet worksheet;

                    if (workbook.Worksheets.Count == 0)
                    {
                        worksheet = workbook.AddWorksheet("Sheet1");
                    }
                    else
                    {
                        worksheet = workbook.Worksheet(1);
                    }

                    int startRow = 9; // Fila inicial para datos
                    int startCol = 1;
                    int initialRow = startRow; // Guardar la fila inicial para el rango de estilos

                    // Llenar con datos
                    foreach (var item in registros)
                    {
                        try
                        {
                            worksheet.Cell(startRow, startCol).Value = startCol;
                            worksheet.Cell(startRow, startCol + 1).Value = item.Folio ?? "";
                            worksheet.Cell(startRow, startCol + 2).Value = item.WasteName ?? "";
                            worksheet.Cell(startRow, startCol + 3).Value = item.Quantity.HasValue ? (double)item.Quantity.Value : 0;
                            worksheet.Cell(startRow, startCol + 4).Value = item.WeightKg.HasValue ? (double)item.WeightKg.Value : 0;

                            // CRETIB - Poner "X" en cada columna individual (columnas 6-11)
                            worksheet.Cell(startRow, startCol + 5).Value = item.Corrosive == true ? "X" : "";
                            worksheet.Cell(startRow, startCol + 6).Value = item.Reactive == true ? "X" : "";
                            worksheet.Cell(startRow, startCol + 7).Value = item.Explosive == true ? "X" : "";
                            worksheet.Cell(startRow, startCol + 8).Value = item.Toxic == true ? "X" : "";
                            worksheet.Cell(startRow, startCol + 9).Value = item.Flammable == true ? "X" : "";
                            worksheet.Cell(startRow, startCol + 10).Value = item.Biological == true ? "X" : "";

                            worksheet.Cell(startRow, startCol + 11).Value = item.GenerationArea ?? "";
                            worksheet.Cell(startRow, startCol + 12).Value = item.GenerationManagerName ?? "";

                            // Manejo correcto de fechas DateOnly
                            if (item.WarehouseEntryDate.HasValue)
                            {
                                var entryDate = item.WarehouseEntryDate.Value;
                                worksheet.Cell(startRow, startCol + 13).Value = new DateTime(entryDate.Year, entryDate.Month, entryDate.Day);
                                worksheet.Cell(startRow, startCol + 13).Style.DateFormat.Format = "dd/MM/yyyy";
                            }
                            else
                            {
                                worksheet.Cell(startRow, startCol + 13).Value = "";
                            }

                            if (item.WarehouseExitDate.HasValue)
                            {
                                var exitDate = item.WarehouseExitDate.Value;
                                worksheet.Cell(startRow, startCol + 14).Value = new DateTime(exitDate.Year, exitDate.Month, exitDate.Day);
                                worksheet.Cell(startRow, startCol + 14).Style.DateFormat.Format = "dd/MM/yyyy";
                            }
                            else
                            {
                                worksheet.Cell(startRow, startCol + 14).Value = "";
                            }

                            worksheet.Cell(startRow, startCol + 15).Value = item.ManifestNumber ?? "";
                            worksheet.Cell(startRow, startCol + 16).Value = item.ManifestDeliveredBy ?? "";
                            worksheet.Cell(startRow, startCol + 17).Value = item.ManifestReceivedBy ?? "";
                            worksheet.Cell(startRow, startCol + 18).Value = item.CollectionTransportName ?? "";
                            worksheet.Cell(startRow, startCol + 19).Value = item.CollectionTransportAuthNumber ?? "";
                            worksheet.Cell(startRow, startCol + 20).Value = item.FinalDisposalName ?? "";
                            worksheet.Cell(startRow, startCol + 21).Value = item.FinalDisposalAuthNumber ?? "";
                            worksheet.Cell(startRow, startCol + 22).Value = item.ManifestSealed == true ? "Sí" : "No";
                            worksheet.Cell(startRow, startCol + 23).Value = item.Comments ?? "";

                            startRow++;
                        }
                        catch (Exception rowEx)
                        {
                            Console.WriteLine($"Error al escribir fila {startRow} (ID: {item.Id}): {rowEx.Message}");
                            Console.WriteLine($"StackTrace: {rowEx.StackTrace}");
                            throw new Exception($"Error al escribir fila {startRow} (ID: {item.Id}): {rowEx.Message}", rowEx);
                        }
                    }

                    // Aplicar estilos
                    try
                    {
                        if (registros.Count > 0)
                        {
                            int lastRow = startRow - 1;
                            int lastColumn = startCol + 23;

                            var dataRange = worksheet.Range(initialRow, startCol, lastRow, lastColumn);
                            dataRange.Style.Font.Bold = true;
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        }
                    }
                    catch (Exception styleEx)
                    {
                        Console.WriteLine($"Error al aplicar estilos: {styleEx.Message}");
                        Console.WriteLine($"StackTrace: {styleEx.StackTrace}");
                        // No lanzamos el error, continuamos con el guardado
                    }

                    try
                    {
                        // Eliminar AutoFilters que pueden causar problemas con ClosedXML
                        if (worksheet.AutoFilter.IsEnabled)
                        {
                            worksheet.AutoFilter.Clear();
                        }

                        using var stream = new MemoryStream();
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        return File(content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    "ResiduosPeligrosos.xlsx");
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"Error al guardar el archivo: {saveEx.Message}");
                        Console.WriteLine($"StackTrace: {saveEx.StackTrace}");
                        return StatusCode(500, $"Error al guardar el archivo: {saveEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el archivo Excel: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Error al generar el archivo Excel: {ex.Message}");
            }
        }

        private bool HazardousWasteManifestExists(int id)
        {
            return _context.HazardousWasteManifests.Any(e => e.Id == id);
        }

        private static List<string> ParseMultiSelectFilter(string? rawValue)
        {
            return (rawValue ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Método para mostrar el historial de residuos peligrosos
        [HttpGet]
        public IActionResult Historial(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            startDate ??= today;
            endDate ??= today.AddDays(1);

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            var historial = _context.HazardousWasteManifests
                .Where(x => x.WarehouseEntryDate >= DateOnly.FromDateTime(startDate.Value) &&
                            x.WarehouseEntryDate <= DateOnly.FromDateTime(endDate.Value))
                .OrderByDescending(x => x.WarehouseEntryDate)
                .ToList();

            return PartialView("Historial", historial);
        }

        // Método para imprimir etiqueta por ID
        [HttpGet]
        public IActionResult PrintLabel(int id)
        {
            try
            {
                var hazardous = _context.HazardousWasteManifests.Find(id);
                if (hazardous == null)
                    return Json(new { success = false, message = "Registro no encontrado" });

                string labelHtml = GenerateLabelHtml(
                    hazardous.Folio ?? "N/A",
                    hazardous.WasteName ?? "N/A",
                    hazardous.WeightKg?.ToString("N4") ?? "0",
                    hazardous.GenerationArea ?? "N/A",
                    hazardous.WarehouseEntryDate?.ToString("dd/MM/yyyy") ?? ""
                );

                return Content(labelHtml, "text/html");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Método para imprimir etiqueta con parámetros directos
        [HttpGet]
        public IActionResult PrintLabelDirect(string folio, string wasteName, string weight, string area, string date)
        {
            if (string.IsNullOrEmpty(folio) || string.IsNullOrEmpty(weight) ||
                string.IsNullOrEmpty(area) || string.IsNullOrEmpty(date))
            {
                return BadRequest("Faltan parámetros requeridos");
            }

            var labelHtml = GenerateLabelHtml(folio, wasteName ?? "N/A", weight, area, date);
            return Content(labelHtml, "text/html");
        }

        // Método para generar HTML de etiqueta de residuo peligroso
        private string GenerateLabelHtml(string folio, string wasteName, string weight, string area, string date)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Etiqueta de Residuo Peligroso</title>
                <style>
                    @page {{
                        size: 4in 2in;
                        margin: 0;
                    }}

                    * {{
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }}

                    body {{
                        font-family: 'Arial', sans-serif;
                        font-size: 9pt;
                        line-height: 1;
                        color: #000;
                        background: white;
                        margin: 0 !important;
                        padding: 0 !important;
                        direction: rtl;      
                        text-align: right;
                    }}

                    .label {{
                        width: 100%;
                        height: 100%;
                        border: 3px solid #ff0000;
                        padding: 10px;
                        margin: 0;
                        background: white;
                        box-sizing: border-box;
                    }}

                    .title {{
                        font-size: 11pt;
                        font-weight: bold;
                        text-align: center;
                        margin-bottom: 8px;
                        padding-bottom: 6px;
                        border-bottom: 2px solid #ff0000;
                        text-transform: uppercase;
                        letter-spacing: 1px;
                        color: #ff0000;
                    }}

                    .content {{
                        font-size: 9pt;
                        line-height: 1.3;
                    }}

                    .field {{
                        margin-bottom: 4px;
                        display: block;
                    }}

                    .field-label {{
                        font-weight: bold;
                        display: inline-block;
                        min-width: 70px;
                        text-align: right;
                    }}

                    .field-value {{
                        display: inline-block;
                        text-align: right;
                    }}

                    .barcode {{
                        margin-top: 12px;
                        height: 25px;
                        background: repeating-linear-gradient(
                            90deg,
                            #000 0px,
                            #000 1px,
                            #fff 1px,
                            #fff 3px
                        );
                        border: 1px solid #000;
                    }}

                    .warning {{
                        margin-top: 10px;
                        text-align: center;
                        font-size: 8pt;
                        color: #ff0000;
                        font-weight: bold;
                    }}

                    @media print {{
                        body {{
                            print-color-adjust: exact;
                            -webkit-print-color-adjust: exact;
                            margin: 0;
                            padding: 0;
                            
                        }}

                        .label {{
                            border: 3px solid #ff0000 !important;
                            background: white !important;
                        }}

                        .no-print {{
                            display: none !important;
                        }}
                    }}

                    @media screen {{
                        body {{
                            background: #f5f5f5;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            
                        }}

                        .label {{
                            width: 3in;
                            height: 2in;
                            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                            background: white;
                        }}
                    }}
                </style>
            </head>
            <body>
                <div class='label'>
                    <div class='title'>Residuo Peligroso </div>
                    <div class='content'>
                        <div class='field'>
                            <span class='field-label'>Residuo:</span>
                            <span class='field-value'>{wasteName}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Fecha:</span>
                            <span class='field-value'>{date}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Área:</span>
                            <span class='field-value'>{area}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Folio:</span>
                            <span class='field-value'>{folio}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Peso:</span>
                            <span class='field-value'>{weight} KG</span>
                        </div>
                    </div>
                </div>

                <script>
                    // Auto-imprimir cuando se abre la ventana
                    window.onload = function() {{
                        // Esperar un momento para que se cargue completamente
                        setTimeout(function() {{
                            window.print();
                        }}, 500);

                        // Cerrar ventana después de imprimir (opcional)
                        window.onafterprint = function() {{
                            window.close();
                        }};
                    }};
                </script>
            </body>
            </html>";
        }
    }
}
