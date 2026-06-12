using OfficeOpenXml;
using OfficeOpenXml.Style;
using RentAFriendApp.Context;
using RentAFriendApp.Models;
using System.Drawing;
using System.IO;
using System.Windows;

namespace RentAFriendApp.Services
{
    public class ExcelExportService
    {
        private readonly string _token;

        public ExcelExportService(string token)
        {
            _token = token;
            ExcelPackage.License.SetNonCommercialPersonal("Nazir Sabitov");
        }
        public async Task<bool> ExportStatisticsAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var stats = AdminContext.GetStatistics(_token).Result;
                    if (stats == null) return false;

                    using var package = new ExcelPackage();
                    var sheet = package.Workbook.Worksheets.Add("Общая информация");

                    // Заголовок
                    sheet.Cells["A1"].Value = "Общая информация системы RentAFriend";
                    sheet.Cells["A1:B1"].Merge = true;
                    StyleHeader(sheet.Cells["A1"], 16, Color.FromArgb(76, 175, 80));

                    // Дата
                    sheet.Cells["A3"].Value = "Дата экспорта:";
                    sheet.Cells["B3"].Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    sheet.Cells["A3"].Style.Font.Bold = true;

                    // Данные
                    int row = 5;
                    AddStatRow(sheet, ref row, "Всего пользователей", stats.TotalUsers.ToString());
                    AddStatRow(sheet, ref row, "Активных пользователей", stats.ActiveUsers.ToString());
                    AddStatRow(sheet, ref row, "Заблокированных пользователей", stats.BlockedUsers.ToString());
                    AddStatRow(sheet, ref row, "Всего бронирований", stats.TotalBookings.ToString());
                    AddStatRow(sheet, ref row, "Общая выручка", $"{stats.TotalRevenue:N2} ₽");
                    AddStatRow(sheet, ref row, "Ожидают верификации", stats.PendingVerifications.ToString());

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();

                    package.SaveAs(new FileInfo(filePath));
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }
        public async Task<bool> ExportLogsAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var response = AuditLogContext.GetAllLogs(_token, page: 1, pageSize: 10000).Result;
                    if (response?.Logs == null || response.Logs.Count == 0) return false;

                    using var package = new ExcelPackage();
                    var sheet = package.Workbook.Worksheets.Add("Логи аудита");

                    // Заголовок
                    sheet.Cells["A1"].Value = "Журнал логов RentAFriend";
                    sheet.Cells["A1:B1"].Merge = true;
                    StyleHeader(sheet.Cells["A1"], 16, Color.FromArgb(76, 175, 80));

                    sheet.Cells["A2"].Value = "Дата экспорта:";
                    sheet.Cells["B2"].Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                    // Шапка таблицы
                    string[] headers = { "ID", "Пользователь (ID)", "Действие", "Таблица", "ID записи",
                                         "Старое значение", "Новое значение", "IP-адрес", "Дата" };
                    int headerRow = 4;

                    for (int col = 0; col < headers.Length; col++)
                    {
                        var cell = sheet.Cells[headerRow, col + 1];
                        cell.Value = headers[col];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.Color.SetColor(Color.White);
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(76, 175, 80));
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    int row = 5;
                    foreach (var log in response.Logs)
                    {
                        sheet.Cells[row, 1].Value = log.LogID;
                        sheet.Cells[row, 2].Value = $"{log.UserName ?? "Система"} ({log.UserID})";
                        sheet.Cells[row, 3].Value = log.Action;
                        sheet.Cells[row, 4].Value = log.TableName;
                        sheet.Cells[row, 5].Value = log.RecordID;
                        sheet.Cells[row, 6].Value = log.OldValue ?? "";
                        sheet.Cells[row, 7].Value = log.NewValue ?? "";
                        sheet.Cells[row, 8].Value = log.IPAddress ?? "-";
                        sheet.Cells[row, 9].Value = log.LoggedAt.ToString("dd.MM.yyyy HH:mm:ss");

                        for (int col = 1; col <= headers.Length; col++)
                        {
                            var cell = sheet.Cells[row, col];
                            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            if (row % 2 == 0)
                            {
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 251));
                            }
                        }

                        sheet.Cells[row, 3].Style.Font.Color.SetColor(GetActionColor(log.Action));
                        sheet.Cells[row, 3].Style.Font.Bold = true;

                        row++;
                    }

                    for (int col = 1; col <= headers.Length; col++)
                        sheet.Column(col).AutoFit();

                    sheet.Column(3).Width = Math.Max(sheet.Column(3).Width, 28);
                    sheet.Column(6).Width = Math.Max(sheet.Column(6).Width, 35);
                    sheet.Column(7).Width = Math.Max(sheet.Column(7).Width, 35);

                    package.SaveAs(new FileInfo(filePath));
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        public async Task<bool> ExportUsersAsync(string filePath, List<UserInfoItem> users)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var package = new ExcelPackage();
                    var sheet = package.Workbook.Worksheets.Add("Пользователи");

                    sheet.Cells["A1"].Value = "Список пользователей RentAFriend";
                    sheet.Cells["A1:G1"].Merge = true;
                    StyleHeader(sheet.Cells["A1"], 16, Color.FromArgb(33, 150, 243));

                    string[] headers = { "ID", "ФИО", "Email", "Телефон", "Роль", "Статус", "Дата регистрации" };
                    int headerRow = 3;

                    for (int col = 0; col < headers.Length; col++)
                    {
                        var cell = sheet.Cells[headerRow, col + 1];
                        cell.Value = headers[col];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.Color.SetColor(Color.White);
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(33, 150, 243));
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    int row = 4;
                    foreach (var user in users)
                    {
                        sheet.Cells[row, 1].Value = user.UserID;
                        sheet.Cells[row, 2].Value = user.FullName;
                        sheet.Cells[row, 3].Value = user.Email;
                        sheet.Cells[row, 4].Value = user.Phone;
                        sheet.Cells[row, 5].Value = user.Role;
                        sheet.Cells[row, 6].Value = user.IsActive ? "Активен" : "Заблокирован";
                        sheet.Cells[row, 7].Value = user.CreatedAt.ToString("dd.MM.yyyy");

                        for (int col = 1; col <= headers.Length; col++)
                        {
                            var cell = sheet.Cells[row, col];
                            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            if (row % 2 == 0)
                            {
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 251));
                            }
                        }

                        row++;
                    }

                    for (int col = 1; col <= headers.Length; col++)
                        sheet.Column(col).AutoFit();

                    package.SaveAs(new FileInfo(filePath));
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        private void AddStatRow(ExcelWorksheet sheet, ref int row, string label, string value)
        {
            sheet.Cells[row, 1].Value = label;
            sheet.Cells[row, 2].Value = value;
            sheet.Cells[row, 1].Style.Font.Bold = true;

            if (row % 2 == 0)
            {
                sheet.Cells[row, 1, row, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 251));
            }
            row++;
        }

        private void StyleHeader(ExcelRange cell, int fontSize, Color color)
        {
            cell.Style.Font.Size = fontSize;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(color);
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private Color GetActionColor(string action)
        {
            if (string.IsNullOrEmpty(action)) return Color.Gray;

            var upper = action.ToUpper();
            if (upper.Contains("DELETE") || upper.Contains("BLOCK")) return Color.FromArgb(244, 67, 54);
            if (upper.Contains("CREATE") || upper.Contains("VERIFY") || upper.Contains("UNBLOCK")) return Color.FromArgb(76, 175, 80);
            if (upper.Contains("UPDATE")) return Color.FromArgb(255, 152, 0);
            if (upper.Contains("LOGIN")) return Color.FromArgb(33, 150, 243);
            return Color.Gray;
        }
    }
}