using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.DTO;
using SGA.Models;
using SGA.Services;

namespace SGA.Controllers
{
    public class ReturnMaterialController : Controller
    {
        private readonly SgaContext _context;
        private readonly LookupService _lookup;

        public ReturnMaterialController(SgaContext context, LookupService lookup)
        {
            _context = context;
            _lookup = lookup;
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        public async Task<IActionResult> Index()
        {
            var partNumbers = await _lookup.GetPartNumberRowsByReturnFlagAsync(true);
            ViewBag.WasteKeyDataList = partNumbers.Select(p => p.PartNumberKey).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.WasteNameDataList = partNumbers.Select(p => p.PartNumberName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.WasteNameGDIDataList = partNumbers.Select(p => p.PartNumberNameGdi).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.PartNumberDataList = partNumbers.Select(p => p.PartNumber1).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.ProgramDataList = partNumbers.Select(p => p.PartNumberProgram).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.AreaDataList = await _lookup.GetAreaDescriptionListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();
            SetModuleViewBags();
            return View("~/Views/NonHazardous/Index.cshtml", await (await _lookup.BuildBaseQueryAsync(true)).ToListAsync());
        }

        [HttpPost]
        public async Task<JsonResult> GetTable()
        {
            try
            {
                int.TryParse(Request.Form["draw"].FirstOrDefault(), out int requestNumber);
                int.TryParse(Request.Form["length"].FirstOrDefault(), out int recordsToTake);
                int.TryParse(Request.Form["start"].FirstOrDefault(), out int recordsToSkip);

                var wasteKeyFilters = ParseMultiSelectFilter(Request.Form["WasteKey"].FirstOrDefault());
                var wasteNameFilters = ParseMultiSelectFilter(Request.Form["WasteName"].FirstOrDefault());
                string wasteNameGdiFilter = Request.Form["WasteNameGDI"].FirstOrDefault() ?? "";
                string partNumberFilter = Request.Form["PartNumber"].FirstOrDefault() ?? "";
                string programFilter = Request.Form["Program"].FirstOrDefault() ?? "";
                string areaGdiFilter = Request.Form["AreaGDI"].FirstOrDefault() ?? "";
                string storageTypeFilter = Request.Form["StorageType"].FirstOrDefault() ?? "";
                string returnToClientFilter = Request.Form["ReturnToClient"].FirstOrDefault() ?? "";
                string sealedManifestsFilter = Request.Form["SealedManifests"].FirstOrDefault() ?? "";

                DateTime? startDateInto = ParseDate(Request.Form["startDateInto"].FirstOrDefault());
                DateTime? endDateInto = ParseDate(Request.Form["endDateInto"].FirstOrDefault());
                DateTime? startDateOut = ParseDate(Request.Form["startDateOut"].FirstOrDefault());
                DateTime? endDateOut = ParseDate(Request.Form["endDateOut"].FirstOrDefault());

                var query = await _lookup.BuildBaseQueryAsync(true);

                if (wasteKeyFilters.Count > 0)
                    query = query.Where(e => e.WasteKey != null && wasteKeyFilters.Contains(e.WasteKey));

                if (wasteNameFilters.Count > 0)
                    query = query.Where(e => e.WasteName != null && wasteNameFilters.Contains(e.WasteName));

                if (!string.IsNullOrWhiteSpace(wasteNameGdiFilter))
                    query = query.Where(e => e.WasteNameGdi != null && e.WasteNameGdi.Contains(wasteNameGdiFilter));

                if (!string.IsNullOrWhiteSpace(partNumberFilter))
                    query = query.Where(e => e.PartNumber != null && e.PartNumber.Contains(partNumberFilter));

                if (!string.IsNullOrWhiteSpace(programFilter))
                    query = query.Where(e => e.Program != null && e.Program.Contains(programFilter));

                if (!string.IsNullOrWhiteSpace(areaGdiFilter))
                    query = query.Where(e => e.AreaGdi != null && e.AreaGdi.Contains(areaGdiFilter));

                if (!string.IsNullOrWhiteSpace(storageTypeFilter))
                    query = query.Where(e => e.StorageType != null && e.StorageType.Contains(storageTypeFilter));

                if (!string.IsNullOrWhiteSpace(returnToClientFilter))
                    query = query.Where(e => e.ReturnToClient != null && e.ReturnToClient.Contains(returnToClientFilter));

                if (!string.IsNullOrWhiteSpace(sealedManifestsFilter))
                    query = query.Where(e => e.SealedManifests != null && e.SealedManifests.Contains(sealedManifestsFilter));

                if (startDateInto.HasValue)
                    query = query.Where(e => e.DateIntoWarehouse >= startDateInto);

                if (endDateInto.HasValue)
                    query = query.Where(e => e.DateIntoWarehouse <= endDateInto);

                if (startDateOut.HasValue)
                    query = query.Where(e => e.DateOutoWarehouse.HasValue && e.DateOutoWarehouse >= startDateOut);

                if (endDateOut.HasValue)
                    query = query.Where(e => e.DateOutoWarehouse.HasValue && e.DateOutoWarehouse <= endDateOut);

                int totalRecords = await (await _lookup.BuildBaseQueryAsync(true)).CountAsync();
                int filteredRecords = await query.CountAsync();

                var data = await query
                    .OrderByDescending(e => e.DateIntoWarehouse)
                    .Skip(recordsToSkip)
                    .Take(recordsToTake)
                    .Select(e => new
                    {
                        e.NonHazardousId,
                        e.WasteKey,
                        e.WasteName,
                        e.WasteNameGdi,
                        e.PartNumber,
                        e.Program,
                        e.WasteQuantity,
                        e.WasteWeight,
                        e.AreaKey,
                        e.AreaGdi,
                        e.DateIntoWarehouse,
                        e.StorageTypeKey,
                        e.StorageType,
                        e.DateOutoWarehouse,
                        e.ManifestNumber,
                        e.WasteDestination,
                        e.ReturnToClient,
                        e.WasteGeneratorNumber,
                        e.CollectorName,
                        e.CollectionAuthorizationNumber,
                        e.CollectionCenterName,
                        e.CollectionCenterAuthorizationNumber,
                        e.ReuseCompanyName,
                        e.ReuseCompanyAuthorizationNumber,
                        e.FinalDisposalCompanyName,
                        e.FinalDisposalAuthorizationNumber,
                        e.SealedManifests,
                        e.Comments
                    })
                    .ToListAsync();

                return Json(new
                {
                    draw = requestNumber,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            string? wasteName,
            string? wasteNameGDI,
            string? partNumber,
            string? program,
            string? areaGDI,
            string? storageType,
            string? startDateInto,
            string? endDateInto,
            string? startDateOut,
            string? endDateOut)
        {
            var plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "BitacoraMaterialRetorno.xlsx");

            try
            {
                var query = await _lookup.BuildBaseQueryAsync(true);

                var wasteKeyFilters = ParseMultiSelectFilter(Request.Query["wasteKey"].FirstOrDefault());
                if (wasteKeyFilters.Count > 0)
                    query = query.Where(r => r.WasteKey != null && wasteKeyFilters.Contains(r.WasteKey));

                var wasteNameFilters = ParseMultiSelectFilter(wasteName);
                if (wasteNameFilters.Count > 0)
                    query = query.Where(r => r.WasteName != null && wasteNameFilters.Contains(r.WasteName));

                if (!string.IsNullOrWhiteSpace(wasteNameGDI))
                    query = query.Where(r => r.WasteNameGdi == wasteNameGDI);

                if (!string.IsNullOrWhiteSpace(partNumber))
                    query = query.Where(r => r.PartNumber == partNumber);

                if (!string.IsNullOrWhiteSpace(program))
                    query = query.Where(r => r.Program == program);

                if (!string.IsNullOrWhiteSpace(areaGDI))
                    query = query.Where(r => r.AreaGdi == areaGDI);

                if (!string.IsNullOrWhiteSpace(storageType))
                    query = query.Where(r => r.StorageType == storageType);

                if (DateTime.TryParse(startDateInto, out DateTime startInto))
                    query = query.Where(r => r.DateIntoWarehouse >= startInto);

                if (DateTime.TryParse(endDateInto, out DateTime endInto))
                    query = query.Where(r => r.DateIntoWarehouse <= endInto);

                if (DateTime.TryParse(startDateOut, out DateTime startOut))
                    query = query.Where(r => r.DateOutoWarehouse >= startOut);

                if (DateTime.TryParse(endDateOut, out DateTime endOut))
                    query = query.Where(r => r.DateOutoWarehouse <= endOut);

                var registros = await query.Take(1000).ToListAsync();
                if (!registros.Any())
                    return NoContent();

                using var workbook = new XLWorkbook(plantillaPath);
                var worksheet = workbook.Worksheets.Count == 0
                    ? workbook.AddWorksheet("Sheet1")
                    : workbook.Worksheet(1);

                int startRow = 6;
                int startCol = 1;

                foreach (var item in registros)
                {
                    worksheet.Cell(startRow, startCol).Value = item.DateIntoWarehouse?.ToString("dd/MM/yyyy");
                    worksheet.Cell(startRow, startCol + 1).Value = item.WasteNameGdi ?? item.WasteName;
                    worksheet.Cell(startRow, startCol + 2).Value = item.PartNumber;
                    worksheet.Cell(startRow, startCol + 3).Value = item.Program;
                    worksheet.Cell(startRow, startCol + 4).Value = item.WasteWeight;
                    worksheet.Cell(startRow, startCol + 5).Value = item.AreaGdi;
                    worksheet.Cell(startRow, startCol + 6).Value = item.Comments;
                    worksheet.Cell(startRow, startCol + 7).Value = item.DateOutoWarehouse?.ToString("dd/MM/yyyy");
                    startRow++;
                }

                if (registros.Count > 0)
                {
                    int lastRow = startRow - 1;
                    int lastColumn = startCol + 7;
                    var dataRange = worksheet.Range(6, 1, lastRow, lastColumn);
                    dataRange.Style.Font.Bold = true;
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                var fileName = $"BitacoraMaterialRetorno_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el archivo Excel: {ex.Message}");
                return StatusCode(500, "Ocurrió un error al generar el archivo.");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await (await _lookup.BuildBaseQueryAsync(true)).FirstOrDefaultAsync(m => m.NonHazardousId == id);
            if (item == null)
                return NotFound();

            SetModuleViewBags();
            return PartialView("~/Views/NonHazardous/Details.cshtml", item);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(true);
            ViewBag.AreaDataList = await _lookup.GetAreaNameListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();
            SetModuleViewBags();
            return View("~/Views/NonHazardous/Create.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Create(NonHazardou model)
        {
            var partNumberInput = model.PartNumber?.Trim();
            var areaInput = model.AreaGdi?.Trim();
            var storageInput = model.StorageType?.Trim();

            var partNumber = await _lookup.GetPartNumberAsync(partNumberInput, true);
            var area = await _lookup.GetAreaAsync(areaInput);
            var storage = await _lookup.GetStorageAsync(storageInput);

            if (partNumber == null)
                ModelState.AddModelError("PartNumber", "Número de parte no válido para material de retorno.");

            if (area == null)
                ModelState.AddModelError("AreaGdi", "Área no válida.");

            if (storage == null)
                ModelState.AddModelError("StorageType", "Tipo de almacenamiento no válido.");

            if (partNumber != null)
            {
                model.WasteKey = partNumber.PartNumberKey;
                model.WasteName = partNumber.PartNumberName;
                model.Program = partNumber.PartNumberProgram;
                model.WasteNameGdi = partNumber.PartNumberNameGdi;
            }

            if (area != null)
                model.AreaKey = area.AreaKey;

            if (storage != null)
                model.StorageTypeKey = storage.StorageKey;

            model.DateIntoWarehouse = DateTime.Now;
            model.ReturnToClient = "Si";
            model.SealedManifests = "No";

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Create));
            }

            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(true);
            ViewBag.AreaDataList = await _lookup.GetAreaNameListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();
            SetModuleViewBags();
            return View("~/Views/NonHazardous/Create.cshtml", model);
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await (await _lookup.BuildBaseQueryAsync(true)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
            if (item == null)
                return NotFound();

            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(true);
            ViewBag.AreaDataList = await _lookup.GetAreaNameListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();
            SetModuleViewBags();
            return View("~/Views/NonHazardous/Edit.cshtml", item);
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NonHazardou formModel)
        {
            if (id != formModel.NonHazardousId)
                return Json(new { success = false, message = "ID no coincide" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Datos inválidos" });

            var dbModel = await (await _lookup.BuildBaseQueryAsync(true)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
            if (dbModel == null)
                return Json(new { success = false, message = "Registro no encontrado" });

            if (await _lookup.GetPartNumberAsync(formModel.PartNumber, true) == null)
                return Json(new { success = false, message = "El número de parte no está marcado como material de retorno." });

            if (formModel.DateOutoWarehouse != null)
            {
                var fechaFormulario = formModel.DateOutoWarehouse.Value.Date;
                var horaActual = DateTime.Now.TimeOfDay;
                dbModel.DateOutoWarehouse = fechaFormulario.Add(horaActual);
            }
            else
            {
                dbModel.DateOutoWarehouse = null;
            }

            dbModel.WasteKey = formModel.WasteKey;
            dbModel.WasteName = formModel.WasteName;
            dbModel.WasteNameGdi = formModel.WasteNameGdi;
            dbModel.PartNumber = formModel.PartNumber;
            dbModel.Program = formModel.Program;
            dbModel.WasteQuantity = formModel.WasteQuantity;
            dbModel.WasteWeight = formModel.WasteWeight;
            dbModel.AreaKey = formModel.AreaKey;
            dbModel.AreaGdi = formModel.AreaGdi;
            dbModel.StorageType = formModel.StorageType;
            dbModel.StorageTypeKey = formModel.StorageTypeKey;
            dbModel.ManifestNumber = formModel.ManifestNumber;
            dbModel.WasteDestination = formModel.WasteDestination;
            dbModel.DateIntoWarehouse = formModel.DateIntoWarehouse;
            dbModel.ReturnToClient = formModel.ReturnToClient;
            dbModel.WasteGeneratorNumber = formModel.WasteGeneratorNumber;
            dbModel.CollectorName = formModel.CollectorName;
            dbModel.CollectionAuthorizationNumber = formModel.CollectionAuthorizationNumber;
            dbModel.CollectionCenterName = formModel.CollectionCenterName;
            dbModel.CollectionCenterAuthorizationNumber = formModel.CollectionCenterAuthorizationNumber;
            dbModel.ReuseCompanyName = formModel.ReuseCompanyName;
            dbModel.ReuseCompanyAuthorizationNumber = formModel.ReuseCompanyAuthorizationNumber;
            dbModel.FinalDisposalCompanyName = formModel.FinalDisposalCompanyName;
            dbModel.FinalDisposalAuthorizationNumber = formModel.FinalDisposalAuthorizationNumber;
            dbModel.SealedManifests = formModel.SealedManifests;
            dbModel.Comments = formModel.Comments;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await (await _lookup.BuildBaseQueryAsync(true)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
            if (item == null)
                return Json(new { success = false, message = "Registro no encontrado." });

            _context.NonHazardous.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<JsonResult> GetWasteData(string partNumber)
        {
            var data = await _lookup.GetPartNumberAsync(partNumber, true);
            if (data == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                wasteKey = data.PartNumberKey,
                wasteName = data.PartNumberName,
                wasteNameGdi = data.PartNumberNameGdi,
                program = data.PartNumberProgram
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetAreaData(string area)
        {
            var data = await _lookup.GetAreaAsync(area);
            if (data == null)
                return Json(new { success = false });

            return Json(new { success = true, areaKey = data.AreaKey });
        }

        [HttpGet]
        public async Task<JsonResult> GetStorageData(string storageName)
        {
            var data = await _lookup.GetStorageAsync(storageName);
            if (data == null)
                return Json(new { success = false });

            return Json(new { success = true, storageTypeKey = data.StorageKey });
        }

        public async Task<IActionResult> Historial(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            startDate ??= today;
            endDate ??= today.AddDays(1);

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            SetModuleViewBags();

            var historial = await (await _lookup.BuildBaseQueryAsync(true))
                .Where(x => x.DateIntoWarehouse >= startDate && x.DateIntoWarehouse < endDate.Value.AddDays(1))
                .OrderByDescending(x => x.DateIntoWarehouse)
                .ToListAsync();

            return PartialView("~/Views/NonHazardous/Historial.cshtml", historial);
        }

        [HttpGet]
        public async Task<IActionResult> PrintLabel(int id)
        {
            var item = await (await _lookup.BuildBaseQueryAsync(true)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
            if (item == null)
                return Json(new { success = false, message = "Registro no encontrado" });

            string labelHtml = GenerateLabelHtml(
                item.PartNumber ?? "",
                item.WasteName ?? item.WasteNameGdi ?? "N/A",
                item.WasteWeight?.ToString("N4") ?? "",
                item.AreaGdi ?? "",
                item.DateIntoWarehouse?.ToString("dd/MM/yyyy") ?? "");

            return Content(labelHtml, "text/html");
        }

        [HttpGet]
        public IActionResult PrintLabelDirect(string partNumber, string wasteName, string weight, string area, string date)
        {
            if (string.IsNullOrEmpty(partNumber) || string.IsNullOrEmpty(weight) ||
                string.IsNullOrEmpty(area) || string.IsNullOrEmpty(date))
            {
                return BadRequest("Faltan parámetros requeridos");
            }

            return Content(GenerateLabelHtml(partNumber, wasteName ?? "N/A", weight, area, date), "text/html");
        }

        private void SetModuleViewBags()
        {
            ViewBag.ModuleTitle = "Material de retorno";
            ViewBag.ModuleEntityLabel = "material de retorno";
            ViewBag.LabelTitle = "Material de Retorno";
            ViewBag.ColWasteKey = "Clave ";
            ViewBag.ColWasteName = "Nombre ";
            ViewBag.ColWasteNameGdi = "Nombre en GDI";
            ViewBag.ColPartNumber = "No. de parte";
            ViewBag.ColProgram = "Programa";
            ViewBag.ColQuantity = "Cantidad (unidad)";
            ViewBag.ColWeight = "Peso en KG";
            ViewBag.ColAreaKey = "Clave del área";
            ViewBag.ColAreaGdi = "Área en GDI";
            ViewBag.ColDateInto = "Fecha de ingreso al almacén";
            ViewBag.ColStorageKey = "Clave de almacenamiento";
            ViewBag.ColStorageType = "Forma de almacenamiento";
            ViewBag.ColComments = "Comentarios";
        }

        private static DateTime? ParseDate(string? value)
            => DateTime.TryParse(value, out var parsed) ? parsed : null;

        private static List<string> ParseMultiSelectFilter(string? rawValue)
        {
            return (rawValue ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string GenerateLabelHtml(string partNumber, string wasteName, string weight, string area, string date)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Etiqueta de Material de Retorno</title>
                <style>
                    @page {{
                        size: 3in 2in landscape;
                        margin: 0;
                    }}

                    * {{
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }}

                    body {{
                        font-family: 'Arial', sans-serif;
                        font-size: 11pt;
                        line-height: 1.3;
                        color: #000;
                        background: white;
                        margin: 0 !important;
                        padding: 0 !important;
                    }}

                    .label {{
                        width: 100%;
                        height: 100%;
                        border: 3px solid #000;
                        padding: 10px;
                        margin: 0;
                        background: white;
                        box-sizing: border-box;
                    }}

                    .title {{
                        font-size: 14pt;
                        font-weight: bold;
                        text-align: center;
                        margin-bottom: 8px;
                        padding-bottom: 6px;
                        border-bottom: 2px solid #000;
                        text-transform: uppercase;
                        letter-spacing: 1px;
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
                    }}

                    .field-value {{
                        display: inline-block;
                    }}

                    @media screen {{
                        body {{
                            background: #f5f5f5;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            padding: 20px;
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
                    <div class='title'>Material de Retorno</div>
                    <div class='content'>
                        <div class='field'>
                            <span class='field-label'>Material:</span>
                            <span class='field-value'>{wasteName}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Fecha:</span>
                            <span class='field-value'>{date}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Area:</span>
                            <span class='field-value'>{area}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>No.Parte:</span>
                            <span class='field-value'>{partNumber}</span>
                        </div>
                        <div class='field'>
                            <span class='field-label'>Peso:</span>
                            <span class='field-value'>{weight} KG</span>
                        </div>
                    </div>
                </div>
                <script>
                    window.onload = function() {{
                        setTimeout(function() {{
                            window.print();
                        }}, 500);
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
