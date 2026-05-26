using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Extensions.Msal;
using SGA.Data;
using SGA.DTO;
using SGA.Models;
using SGA.Services;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Net.Sockets;
using System.Text;
using System.Drawing.Printing;
using System.Drawing;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Data.Common;




namespace SGA.Controllers
{
    public class NonHazardousController : Controller
    {
        private readonly SgaContext _context;
        private readonly LookupService _lookup;

        public NonHazardousController(SgaContext context, LookupService lookup)
        {
            _context = context;
            _lookup = lookup;

        }
        [Authorize(Roles = "Administrator,Supervisor")]
        // GET: NonHazardous
        public async Task<IActionResult> Index()
        {
            var partNumbers = await _lookup.GetPartNumberRowsByReturnFlagAsync(false);
            ViewBag.WasteKeyDataList = partNumbers.Select(p => p.PartNumberKey).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.WasteNameDataList = partNumbers.Select(p => p.PartNumberName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.WasteNameGDIDataList = partNumbers.Select(p => p.PartNumberNameGdi).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.PartNumberDataList = partNumbers.Select(p => p.PartNumber1).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.ProgramDataList = partNumbers.Select(p => p.PartNumberProgram).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ViewBag.AreaDataList = await _lookup.GetAreaDescriptionListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();

            return View(await (await _lookup.BuildBaseQueryAsync(false)).ToListAsync());
        }

        [HttpPost]
        public async Task<JsonResult> GetTable()
        {
            try
            {
                int.TryParse(Request.Form["draw"].FirstOrDefault(), out int NroPeticion);
                int.TryParse(Request.Form["length"].FirstOrDefault(), out int CantidadRegistros);
                int.TryParse(Request.Form["start"].FirstOrDefault(), out int OmitirRegistros);


                // Obtener filtros adicionales desde los parámetros
                var wasteKeyFilters = ParseMultiSelectFilter(Request.Form["WasteKey"].FirstOrDefault());

                var wasteNameFilters = ParseMultiSelectFilter(Request.Form["WasteName"].FirstOrDefault());
                string wasteNameGDIFilter = Request.Form["WasteNameGDI"].FirstOrDefault() ?? "";
                string partNumberFilter = Request.Form["PartNumber"].FirstOrDefault() ?? "";
                string programFilter = Request.Form["Program"].FirstOrDefault() ?? "";
                string areaGDIFilter = Request.Form["AreaGDI"].FirstOrDefault() ?? "";
                string storageTypeFilter = Request.Form["StorageType"].FirstOrDefault() ?? "";
                string returnToClientFilter = Request.Form["ReturnToClient"].FirstOrDefault() ?? "";
                string sealedManifestsFilter = Request.Form["SealedManifests"].FirstOrDefault() ?? "";
                string dateIntoWarehouseFilter = Request.Form["DateIntoWarehouse"].FirstOrDefault() ?? "";
                string dateOutWarehouseFilter = Request.Form["DateOutWarehouse"].FirstOrDefault() ?? "";

                // Obtener rango de fechas desde los parámetros
                string startDateIntoStr = Request.Form["startDateInto"].FirstOrDefault();
                string endDateIntoStr = Request.Form["endDateInto"].FirstOrDefault();
                string startDateOutStr = Request.Form["startDateOut"].FirstOrDefault();
                string endDateOutStr = Request.Form["endDateOut"].FirstOrDefault();
                DateTime? startDateInto = null, endDateInto = null, startDateOut = null, endDateOut = null;
                if (DateTime.TryParse(startDateIntoStr, out DateTime parsedStartDateInto))
                {
                    startDateInto = parsedStartDateInto;
                }
                if (DateTime.TryParse(endDateIntoStr, out DateTime parsedEndDateIntoStr))
                {
                    endDateInto = parsedEndDateIntoStr;
                }
                if (DateTime.TryParse(startDateOutStr, out DateTime parsedStartDateOut))
                {
                    startDateOut = parsedStartDateOut;
                }
                if (DateTime.TryParse(endDateOutStr, out DateTime parsedEndDateOut))
                {
                    endDateOut = parsedEndDateOut;
                }

             

                // Base query
                var queryEmpleado = await _lookup.BuildBaseQueryAsync(false);



                // Filtros adicionales
                if (wasteKeyFilters.Count > 0)
                {
                    queryEmpleado = queryEmpleado.Where(e => e.WasteKey != null && wasteKeyFilters.Contains(e.WasteKey));
                }

                if (wasteNameFilters.Count > 0)
                {
                    queryEmpleado = queryEmpleado.Where(e => e.WasteName != null && wasteNameFilters.Contains(e.WasteName));
                }

                if (!string.IsNullOrEmpty(wasteNameGDIFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.WasteNameGdi.Contains(wasteNameGDIFilter)); // Ajusta según la lógica real
                }

                if (!string.IsNullOrEmpty(partNumberFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.PartNumber.Contains(partNumberFilter));
                }

                if (!string.IsNullOrEmpty(programFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.Program.Contains(programFilter)); // Ajusta el campo según lo necesario
                }

                if (!string.IsNullOrEmpty(areaGDIFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.AreaGdi.Contains(areaGDIFilter)); // Ajusta el campo según lo necesario
                }

                if (!string.IsNullOrEmpty(storageTypeFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.StorageType.Contains(storageTypeFilter)); // Ajusta el campo según lo necesario
                }

                if (!string.IsNullOrEmpty(returnToClientFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.ReturnToClient.Contains(returnToClientFilter)); // Ajusta el campo según lo necesario
                }

                if (!string.IsNullOrEmpty(sealedManifestsFilter))
                {
                    queryEmpleado = queryEmpleado.Where(e => e.SealedManifests.Contains(sealedManifestsFilter)); // Ajusta el campo según lo necesario
                }

                if(startDateInto.HasValue || endDateInto.HasValue)
                {
                    if (startDateInto.HasValue)
                        queryEmpleado = queryEmpleado.Where(e => e.DateIntoWarehouse >= startDateInto);

                    if (endDateInto.HasValue)
                        queryEmpleado = queryEmpleado.Where(e => e.DateIntoWarehouse <= endDateInto);
                }

                if (startDateOut.HasValue || endDateOut.HasValue)
                {
                    if (startDateOut.HasValue)
                        queryEmpleado = queryEmpleado.Where(e => e.DateOutoWarehouse.HasValue &&
                                                               e.DateOutoWarehouse >= startDateOut);

                    if (endDateOut.HasValue)
                        queryEmpleado = queryEmpleado.Where(e => e.DateOutoWarehouse.HasValue &&
                                                               e.DateOutoWarehouse <= endDateOut);
                }




                // Total registros antes y después del filtro
                int TotalRegistros = await (await _lookup.BuildBaseQueryAsync(false)).CountAsync();
                int TotalRegistrosFiltrados = await queryEmpleado.CountAsync();
                // Ordenar por fecha y hora de manera descendente
                queryEmpleado = queryEmpleado.OrderByDescending(e => e.DateIntoWarehouse);
                                             


        var lista = await queryEmpleado
            .Skip(OmitirRegistros)
            .Take(CantidadRegistros)
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
        string? endDateOut
        )
        {

            var plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Bitacora.xlsx");
            if (System.IO.File.Exists(plantillaPath))
            {
                Console.WriteLine($"Archivo encontrado en: {plantillaPath}");
                // Procede con la carga o la operación con el archivo
            }
            else
            {
                Console.WriteLine("El archivo no existe en la ruta especificada.");
            }

            Console.WriteLine($"Ruta de la plantilla: {plantillaPath}");
           


            try
            {
                // Consulta con filtros dinámicos
                IQueryable<NonHazardou> query = await _lookup.BuildBaseQueryAsync(false);

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


                // Evitar problemas de memoria con grandes cantidades de datos
                var registros = await query.Take(1000).ToListAsync(); // Limitar la cantidad de registros a 1000 por ejemplo

                if (!registros.Any())
                {
                    return NoContent(); // Devuelve un 204 si no hay registros
                }

                // Crear el archivo Excel
                using (var workbook = new XLWorkbook(plantillaPath))
                {

                    IXLWorksheet worksheet;

                    if (workbook.Worksheets.Count == 0)
                    {
                        worksheet = workbook.AddWorksheet("Sheet1"); // Si no hay hojas, se crea una nueva
                    }
                    else
                    {
                        worksheet = workbook.Worksheet(1); // Selecciona la primera hoja
                    }
                    int startRow = 11; // Empieza a insertar datos desde la fila 5
                    int startCol = 1; // Empieza desde la columna 
                    

                    // Llenar con datos
                    foreach (var item in registros)
                    {
                        worksheet.Cell(startRow, startCol).Value = item.NonHazardousId;
                        worksheet.Cell(startRow, startCol + 1).Value = item.WasteKey;
                        worksheet.Cell(startRow, startCol + 2).Value = item.WasteName;
                        worksheet.Cell(startRow, startCol + 3).Value = item.WasteNameGdi;
                        worksheet.Cell(startRow, startCol + 4).Value = item.PartNumber;
                        worksheet.Cell(startRow, startCol + 5).Value = item.Program;
                        worksheet.Cell(startRow, startCol + 6).Value = item.WasteQuantity;
                        worksheet.Cell(startRow, startCol + 7).Value = item.WasteWeight;
                        worksheet.Cell(startRow, startCol + 8).Value = item.AreaKey;
                        worksheet.Cell(startRow, startCol + 9).Value = item.AreaGdi;
                        worksheet.Cell(startRow, startCol + 10).Value = item.DateIntoWarehouse?.ToString("dd/MM/yyyy");
                        worksheet.Cell(startRow, startCol + 11).Value = item.StorageType;
                        worksheet.Cell(startRow, startCol + 12).Value = item.DateOutoWarehouse?.ToString("dd/MM/yyyy");
                        worksheet.Cell(startRow, startCol + 13).Value = item.ManifestNumber;
                        worksheet.Cell(startRow, startCol + 14).Value = item.WasteDestination;
                        worksheet.Cell(startRow, startCol + 15).Value = item.ReturnToClient;
                        worksheet.Cell(startRow, startCol + 16).Value = item.WasteGeneratorNumber;
                        worksheet.Cell(startRow, startCol + 17).Value = item.CollectorName;
                        worksheet.Cell(startRow, startCol + 18).Value = item.CollectionAuthorizationNumber;
                        worksheet.Cell(startRow, startCol + 19).Value = item.CollectionCenterName;
                        worksheet.Cell(startRow, startCol + 20).Value = item.CollectionCenterAuthorizationNumber;
                        worksheet.Cell(startRow, startCol + 21).Value = item.ReuseCompanyName;
                        worksheet.Cell(startRow, startCol + 22).Value = item.ReuseCompanyAuthorizationNumber;
                        worksheet.Cell(startRow, startCol + 23).Value = item.FinalDisposalCompanyName;
                        worksheet.Cell(startRow, startCol + 24).Value = item.FinalDisposalAuthorizationNumber;
                        worksheet.Cell(startRow, startCol + 25).Value = item.SealedManifests;
                        worksheet.Cell(startRow, startCol + 26).Value = item.Comments;
                        startRow++; // Avanza a la siguiente fila
                    }
                    // 2. Aplicar estilos DESPUÉS de insertar los datos
                    if (registros.Count > 0)
                    {
                        int lastRow = startRow - 1; // La última fila con datos
                        int lastColumn = startCol + 26;

                        // Usar la sobrecarga correcta de Range
                        var dataRange = worksheet.Range(
                            11,          // startRow
                            1,           // startColumn
                            lastRow,     // endRow
                            lastColumn   // endColumn
                        );

                        dataRange.Style.Font.Bold = true;
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    }



                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    var content = stream.ToArray(); // aquí ya no hay stream abierto

                    return File(content,
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "ResiduosNoPeligrosos.xlsx");

                }
            }
            catch (Exception ex)
            {
                // Log de error más detallado
                Console.WriteLine($"Error al generar el archivo Excel: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, "Error al generar el archivo Excel.");
            }
        }









        // GET: NonHazardous/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var nonHazardou = await _context.NonHazardous
                .Where(m => m.NonHazardousId == id)
                .FirstOrDefaultAsync();
            var allowedIds = await (await _lookup.BuildBaseQueryAsync(false))
                .Where(m => m.NonHazardousId == id)
                .Select(m => m.NonHazardousId)
                .AnyAsync();
            if (nonHazardou == null || !allowedIds) return NotFound();

            return PartialView(nonHazardou);
        }

        // GET: NonHazardous/Create
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(false);
            ViewBag.AreaDataList = await _lookup.GetAreaNameListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NonHazardou model)
        {
           
            var partNumberInput = model.PartNumber?.Trim();
            var areaInput = model.AreaGdi?.Trim();
            var storageInput = model.StorageType?.Trim();

            var partNumber = await _lookup.GetPartNumberAsync(partNumberInput, false);
            var area = await _lookup.GetAreaAsync(areaInput);
            var storage = await _lookup.GetStorageAsync(storageInput);


            // Validaciones
            if (partNumber == null)
                ModelState.AddModelError("PartNumber", "Número de parte no válido.");

            if (area == null)
                ModelState.AddModelError("AreaGdi", "Área no válida.");

            if (storage == null)
                ModelState.AddModelError("StorageType", "Tipo de almacenamiento no válido.");

            // Asignar datos al modelo
            if (partNumber != null)
            {
                model.WasteKey = partNumber.PartNumberKey;
                model.WasteName = partNumber.PartNumberName;
                model.Program = partNumber.PartNumberProgram;
                model.WasteNameGdi = partNumber.PartNumberNameGdi;
            }

            if (area != null)
            {
                model.AreaKey = area.AreaKey;

            }

            if (storage != null)
            {
                model.StorageTypeKey = storage.StorageKey;
            }

            model.DateIntoWarehouse = DateTime.Now;
            model.ReturnToClient = "No";
            model.SealedManifests = "No";


            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Create));
            }

            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(false);
            ViewBag.AreaDataList = await _lookup.GetAreaDescriptionListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();

            return View(model);
        }


        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpGet]
        // GET: NonHazardous/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nonHazardou = await (await _lookup.BuildBaseQueryAsync(false)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
            if (nonHazardou == null)
            {
                return NotFound();
            }
            ViewBag.PartNumberDataList = await _lookup.GetPartNumberListAsync(false);
            ViewBag.AreaDataList = await _lookup.GetAreaNameListAsync();
            ViewBag.StorageDataList = await _lookup.GetStorageNameListAsync();
            return View("Edit",nonHazardou);
        }

        // POST: NonHazardous/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NonHazardou formModel)
        {
            if (id != formModel.NonHazardousId)
                return Json(new { success = false, message = "ID no coincide" });

            if (ModelState.IsValid)
            {
                var dbModel = await (await _lookup.BuildBaseQueryAsync(false)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
                if (dbModel == null)
                    return Json(new { success = false, message = "Registro no encontrado" });

                if (formModel.DateOutoWarehouse != null)
                {
                    var fechaFormulario = formModel.DateOutoWarehouse.Value.Date;
                    var horaActual = DateTime.Now.TimeOfDay;
                    dbModel.DateOutoWarehouse = fechaFormulario.Add(horaActual);
                }

                // Actualiza campos...
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

            return Json(new { success = false, message = "Datos inválidos" });
        }


        [Authorize(Roles = "Administrator,Supervisor")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await (await _lookup.BuildBaseQueryAsync(false)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Registro no encontrado." });
                }

                _context.NonHazardous.Remove(item);
                await _context.SaveChangesAsync();

                return Json(new { success = true });


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetWasteData(string partNumber)
        {
            var data = await _lookup.GetPartNumberAsync(partNumber, false);

            if (data == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                wasteKey = data.PartNumberKey,
                wasteName = data.PartNumberName,
                wasteNameGdi = data.PartNumberNameGdi,
                program = data.PartNumberProgram,
            });
        }
        [HttpGet]
        public async Task<JsonResult> GetAreaData(string area)
        {
            var data = await _lookup.GetAreaAsync(area);
            if (data == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                areaKey = data.AreaKey
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetStorageData(string storageName)
        {
            var data = await _lookup.GetStorageAsync(storageName);
            if (data == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                storageTypeKey = data.StorageKey
            });
        }


        public async Task<IActionResult> Historial(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;

            // Establecer valores predeterminados
            startDate ??= today;
            endDate ??= today.AddDays(1);

            // Guardar fechas en ViewBag para la vista
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            var historial = await (await _lookup.BuildBaseQueryAsync(false))
                .Where(x => x.DateIntoWarehouse >= startDate &&
                            x.DateIntoWarehouse < endDate.Value.AddDays(1))
                .OrderByDescending(x => x.DateIntoWarehouse)
                .ToListAsync();

            return PartialView("Historial", historial);
        }





        private bool NonHazardouExists(int id)
        {
            return _context.NonHazardous.Any(e => e.NonHazardousId == id);
        }

        private static List<string> ParseMultiSelectFilter(string? rawValue)
        {
            return (rawValue ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        [HttpGet]
        public async Task<IActionResult> PrintLabel(int id)
        {
            try
            {
                var nonHazardous = await (await _lookup.BuildBaseQueryAsync(false)).FirstOrDefaultAsync(x => x.NonHazardousId == id);
                if (nonHazardous == null)
                    return Json(new { success = false, message = "Registro no encontrado" });

                // Generar HTML en lugar de ZPL para impresión del navegador
                string labelHtml = GenerateLabelHtml(
                    nonHazardous.PartNumber,
                    nonHazardous.WasteName ?? nonHazardous.WasteNameGdi ?? "N/A",
                    nonHazardous.WasteWeight?.ToString("N4") ?? "",
                    nonHazardous.AreaGdi,
                    nonHazardous.DateIntoWarehouse?.ToString("dd/MM/yyyy") ?? ""
                );

                return Content(labelHtml, "text/html");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PrintLabelDirect(string partNumber, string wasteName, string weight, string area, string date)
        {
            if (string.IsNullOrEmpty(partNumber) || string.IsNullOrEmpty(weight) ||
                string.IsNullOrEmpty(area) || string.IsNullOrEmpty(date))
            {
                return BadRequest("Faltan parámetros requeridos");
            }

            var labelHtml = GenerateLabelHtml(partNumber, wasteName ?? "N/A", weight, area, date);
            return Content(labelHtml, "text/html");
        }

        // Método para generar texto plano (mantener tu método original si lo necesitas)
        private string GenerateLabel(string PartNumber, string Weight, string Area, string Date)
        {
            return $@"Residuo
                Fecha: {Date}
                Area: {Area}
                DNo.Parte: {PartNumber}
                Peso: {Weight} KG";
        }

        // Nuevo método para generar HTML optimizado para impresión
        private string GenerateLabelHtml(string partNumber, string wasteName, string weight, string area, string date)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Etiqueta de Residuo No Peligroso</title>
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
        
                    .footer {{
                        margin-top: 10px;
                        text-align: center;
                        font-size: 8pt;
                        color: #666;
                    }}
        
                    @media print {{
                        body {{
                            print-color-adjust: exact;
                            -webkit-print-color-adjust: exact;
                            margin: 0;
                            padding: 0;
                        }}
            
                        .label {{
                            border: 3px solid #000 !important;
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
                    <div class='title'>Residuo</div>
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
