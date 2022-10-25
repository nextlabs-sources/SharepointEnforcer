using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenXml = DocumentFormat.OpenXml;
using Package = DocumentFormat.OpenXml.Packaging;
using WordProcess = DocumentFormat.OpenXml.Wordprocessing;
using Presentation = DocumentFormat.OpenXml.Presentation;
using Spreadsheet = DocumentFormat.OpenXml.Spreadsheet;
using Draw = DocumentFormat.OpenXml.Drawing;
using Word = Microsoft.Office.Interop.Word;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Excel = Microsoft.Office.Interop.Excel;
using Core = Microsoft.Office.Core;
using PdfEditorLib;
using NextLabs.Diagnostic;

namespace NextLabs.Common
{
    public class SPAddHeader
    {
        // parameter "values": one value means one "Add header" obligation.
        // Add header using "DocumentFormat.OpenXml". If header exist "itar__no" and add "itar__yes", we need change it to "itar__yes".
        static public bool AddHeaderForLocalWordFile(string filePath, string position, List<string> values)
        {
            try
            {
                using (Package.WordprocessingDocument package = Package.WordprocessingDocument.Open(filePath, true))
                {
                    Package.MainDocumentPart docPart = package.MainDocumentPart;
                    bool bExisted = false;
                    foreach (Package.HeaderPart headerPart in docPart.HeaderParts)
                    {
                        if (headerPart != null)
                        {
                            bool bRun = SetHeaderValue(headerPart, position, values);
                            if (bRun)
                            {
                                bExisted = true;
                            }
                        }
                    }
                    if (!bExisted)
                    {
                        NewHeaderPart(docPart, position, values);
                    }
                    docPart.Document.Save();
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForLocalWordFile:", null, ex);
            }
            return false;
        }

        public static bool AddHeaderForExcel2007(string filePath, string strPosition, List<string> values)
        {
            bool bret = false;
            try
            {
                using (Package.SpreadsheetDocument excelDoc = Package.SpreadsheetDocument.Open(filePath, true))
                {
                    Package.WorkbookPart wbPart = excelDoc.WorkbookPart;
                    foreach (Spreadsheet.Sheet sheet in wbPart.Workbook.Sheets)
                    {
                        Package.WorksheetPart wsPart = (Package.WorksheetPart)(wbPart.GetPartById(sheet.Id));
                        Spreadsheet.Worksheet ws = wsPart.Worksheet;

                        if (ws == null)
                        {
                            return false;
                        }
                        Spreadsheet.HeaderFooter hf = ws.Descendants<Spreadsheet.HeaderFooter>().FirstOrDefault();
                        if (hf == null)
                        {
                            hf = new Spreadsheet.HeaderFooter();
                            ws.AppendChild<Spreadsheet.HeaderFooter>(hf);
                        }
                        Dictionary<int, object> headerDic = GetAllHeaderDic(hf);
                        foreach (KeyValuePair<int, object> kvp in headerDic)
                        {
                            string headerText = GetHeaderText(kvp);
                            string leftHeader = "";
                            string centerHeader = "";
                            string rightHeader = "";
                            GetExcelThreePosHeader(headerText, ref leftHeader, ref centerHeader, ref rightHeader);
                            if (strPosition.Equals("left", StringComparison.OrdinalIgnoreCase))
                            {
                                MergeHeaderToTarget(ref leftHeader, ref centerHeader, values);
                                MergeHeaderToTarget(ref leftHeader, ref rightHeader, values);
                                HandleHeaderText(ref leftHeader, values);
                            }
                            else if (strPosition.Equals("center", StringComparison.OrdinalIgnoreCase))
                            {
                                MergeHeaderToTarget(ref centerHeader, ref leftHeader, values);
                                MergeHeaderToTarget(ref centerHeader, ref rightHeader, values);
                                HandleHeaderText(ref centerHeader, values);
                            }
                            else
                            {
                                MergeHeaderToTarget(ref rightHeader, ref leftHeader, values);
                                MergeHeaderToTarget(ref rightHeader, ref centerHeader, values);
                                HandleHeaderText(ref rightHeader, values);
                            }
                            headerText = "&L" + leftHeader + "&C" + centerHeader + "&R" + rightHeader;
                            if (headerText.Length > 255)
                            {
                                headerText = headerText.Substring(0, 255);
                            }
                            SetAllHeaderText(kvp, headerText);
                        }
                        ws.Save();
                    }
                    excelDoc.Close();
                    bret = true;
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForExcel2007_2:", null, ex);
            }
            return bret;
        }

        public static bool AddFooterForPPT2007(string filePath, string position, List<string> values)
        {
            bool bret = false;
            try
            {
                using (Package.PresentationDocument pptDocument = Package.PresentationDocument.Open(filePath, true))
                {
                    Package.PresentationPart presentationPart = pptDocument.PresentationPart;
                    // Verify that the presentation part and presentation exist.
                    if (presentationPart != null && presentationPart.Presentation != null)
                    {
                        foreach (Package.SlidePart slidePart in presentationPart.SlideParts)
                        {
                            Presentation.Slide slide = slidePart.Slide;
                            Presentation.ShapeTree tree = slide.CommonSlideData.ShapeTree;
                            SetPPT2007FooterText(tree, position, values);
                            slide.Save();
                        }
                    }
                    bret = true;
                    pptDocument.Close();
                }
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForPPT2007:", null, ex);
            }
            return bret;
        }

        static public bool AddHeaderFooterForPPT2003(string filePath, string strPosition, List<string> values)
        {
            bool result = false;
            PowerPoint._Application pptApp = null;
            PowerPoint.Presentation presentation = null;
            try
            {
                pptApp = new PowerPoint.Application();
                presentation = pptApp.Presentations.Open(filePath, Core.MsoTriState.msoFalse, Core.MsoTriState.msoFalse, Core.MsoTriState.msoFalse);
                Core.MsoTextEffectAlignment alignment = GetHeaderPosForPPT2003(strPosition);
                foreach (PowerPoint.Slide slide in presentation.Slides)
                {
                    PowerPoint.HeadersFooters headersFooters = slide.HeadersFooters;
                    if (headersFooters != null && headersFooters.Footer != null)
                    {
                        if (headersFooters.Footer.Visible == Core.MsoTriState.msoFalse)
                        {
                            headersFooters.Footer.Visible = Core.MsoTriState.msoTrue;
                        }
                        string footerText = headersFooters.Footer.Text;
                        HandleHeaderText(ref footerText, values);
                        headersFooters.Footer.Text = footerText;
                        foreach (PowerPoint.Shape shape in slide.Shapes)
                        {
                            if (shape.Type == Core.MsoShapeType.msoPlaceholder && shape.PlaceholderFormat != null && shape.PlaceholderFormat.Type == PowerPoint.PpPlaceholderType.ppPlaceholderFooter)
                            {
                                shape.TextEffect.Alignment = alignment;
                                break;
                            }
                        }
                    }
                }
                presentation.Save();
                result = true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForPPT2003:", null, ex);
            }
            finally
            {
                // close document & ppt app
                if (presentation != null)
                {
                    try
                    {
                        presentation.Close();
                    }
                    catch { }
                }
                if (pptApp != null)
                {
                    try
                    {
                        pptApp.Quit();
                    }
                    catch { }
                }

            }
            return result;
        }

        public static bool AddHeaderForWord2003(string filePath, string position, List<string> values)
        {
            Word._Application wordApp = new Word.Application();
            wordApp.Visible = false;
            Word.Document wordDoc = null;
            bool bret = false;
            try
            {
                object filename = filePath;
                wordDoc = wordApp.Documents.Open(ref filename);
                Word.WdParagraphAlignment headerPos = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                if (position.Equals("Right", StringComparison.OrdinalIgnoreCase))
                {
                    headerPos = Word.WdParagraphAlignment.wdAlignParagraphRight;
                }
                else if (position.Equals("Center", StringComparison.OrdinalIgnoreCase))
                {
                    headerPos = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }
                foreach (Word.Section section in wordDoc.Sections)
                {
                    foreach (Word.HeaderFooter header in section.Headers)
                    {
                        header.Range.ParagraphFormat.Alignment = headerPos;
                        string curHeaderValue = header.Range.Text;
                        HandleHeaderText(ref curHeaderValue, values);
                        header.Range.Text = curHeaderValue;
                    }
                }
                wordDoc.Save();
                bret = true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForWord2003:", null, ex);
            }
            finally
            {
                if (wordDoc != null)
                {
                    try
                    {
                        wordDoc.Close(Word.WdSaveOptions.wdPromptToSaveChanges);
                    }
                    catch { }
                }
                if (wordApp != null)
                {
                    try
                    {
                        wordApp.Quit();
                    }
                    catch { }
                }
            }
            return bret;
        }

        public static bool AddHeaderForExcel2003(string filePath, string strPosition, List<string> values)
        {
            Excel._Application excelApp = new Excel.Application();
            excelApp.Visible = false;
            Excel.Workbook workBook = null;
            bool bret = false;
            try
            {
                string fileSuffix = Globals.GetFileSuffix(filePath);
                if (fileSuffix.Equals("xlt", StringComparison.OrdinalIgnoreCase))
                {
                    workBook = excelApp.Workbooks.Open(filePath, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, true, Type.Missing, Type.Missing, true);
                }
                else
                {
                    workBook = excelApp.Workbooks.Open(filePath);
                }
                foreach (Excel.Worksheet workSheet in workBook.Sheets)
                {
                    if (strPosition.Equals("left", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleExcelAllLeftHeader(workSheet, values);
                    }
                    else if (strPosition.Equals("center", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleExcelAllCenterHeader(workSheet, values);
                    }
                    else if (strPosition.Equals("right", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleExcelAllRightHeader(workSheet, values);
                    }
                }
                workBook.CheckCompatibility = false;
                workBook.Save();
                bret = true;
            }
            catch (Exception ex)
            {
                NLLogger.OutputLog(LogLevel.Debug, "Exception during AddHeaderForExcel2003:", null, ex);
            }
            finally
            {
                if (workBook != null)
                {
                    try
                    {
                        workBook.Close();
                    }
                    catch { }
                }
                if (excelApp != null)
                {
                    try
                    {
                        excelApp.Quit();
                    }
                    catch { }
                }
            }
            return bret;
        }

        public static bool AddHeaderForPdf(string filePath, string strPosition, List<string> values)
        {
            try
            {
                string targetText = "";
                foreach (string value in values)
                {
                    bool bExisted = false;
                    string newValue = SetHeaderText(targetText, value, ref bExisted);
                    if (!bExisted)
                    {
                        targetText = targetText + " " + value;
                    }
                    else if (!newValue.Equals(targetText))
                    {
                        targetText = newValue;
                    }
                }
                PdfHeaderFooter headerFooter = new PdfHeaderFooter();
                headerFooter.AddHeaderText(filePath, targetText, strPosition, 10, 10);
            }
            catch
            {
                return false;
            }
            return true;
        }


        private static bool SetHeaderValue(Package.HeaderPart headerPart, string position, List<string> values)
        {
            WordProcess.JustificationValues headerPos = GetJustificationPosition(position);
            WordProcess.Header header = headerPart.Header;
            if (!string.IsNullOrEmpty(header.InnerText)) // We don't do anything when header text is empty or null in this.
            {
                foreach (string value in values)
                {
                    bool bExisted = false;
                    WordProcess.Paragraph lastChild = null;

                    foreach (OpenXml.OpenXmlElement element in header.ChildElements)
                    {
                        if (element is WordProcess.Paragraph)
                        {
                            WordProcess.Paragraph paragraph = element as WordProcess.Paragraph;
                            string oldValue = string.Empty;
                            foreach (WordProcess.Run run in paragraph.Elements<WordProcess.Run>())
                            {
                                if (string.IsNullOrEmpty(run.InnerText))
                                {
                                    oldValue += " ";
                                }
                                else
                                {
                                    oldValue += run.InnerText;
                                }
                            }
                            string newValue = SetHeaderText(oldValue, value, ref bExisted);
                            if (!newValue.Equals(oldValue))
                            {
                                paragraph.InnerXml = NewParagraph(newValue, headerPos).InnerXml; //replace the XML using new Paragraph
                            }
                            else
                            {
                                WordProcess.ParagraphProperties props = paragraph.ParagraphProperties;
                                if (props == null)
                                {
                                    props = new WordProcess.ParagraphProperties();
                                    paragraph.ParagraphProperties = props;
                                }
                                if (props.Justification != null)
                                {
                                    props.Justification.Val = headerPos;
                                }
                                else
                                {
                                    WordProcess.Justification newJc = new WordProcess.Justification();
                                    newJc.Val = headerPos;
                                    props.Justification = newJc;
                                }
                            }
                            lastChild = paragraph;
                        }
                    }
                    if (!bExisted)
                    {
                        if (lastChild != null)
                        {
                            string oldHeader = string.Empty;
                            foreach (WordProcess.Run run in lastChild.Elements<WordProcess.Run>())
                            {
                                if (string.IsNullOrEmpty(run.InnerText))
                                {
                                    oldHeader += " ";
                                }
                                else
                                {
                                    oldHeader += run.InnerText;
                                }
                            }
                            string newValue = oldHeader + " " + value; // Add the "value" in the end.
                            lastChild.InnerXml = NewParagraph(newValue, headerPos).InnerXml;
                        }
                        else
                        {
                            header.Append(NewParagraph(value, headerPos));
                        }
                    }
                }
                header.Save();
                return true;
            }
            return false;
        }

        // return the new Paragraph.
        private static WordProcess.Paragraph NewParagraph(string text, WordProcess.JustificationValues paraPos)
        {
            WordProcess.Paragraph newParagraph =
                new WordProcess.Paragraph(
                    new WordProcess.ParagraphProperties(
                        new WordProcess.ParagraphStyleId() { Val = "Header" },
                        new WordProcess.Justification() { Val = paraPos }),
                    new WordProcess.Run(
                        new WordProcess.Text(text)));
            return newParagraph;
        }

        private static void NewHeaderPart(Package.MainDocumentPart docPart, string position, List<string> values)
        {
            string headerText = "";
            foreach (string value in values)
            {
                bool bExisted = false;
                headerText = SetHeaderText(headerText, value, ref bExisted);
                if (!bExisted)
                {
                    headerText = headerText + " " + value;
                }
            }
            WordProcess.JustificationValues headerPos = GetJustificationPosition(position);
            Package.HeaderPart headrPart = docPart.AddNewPart<Package.HeaderPart>();
            WordProcess.Header header =
                new WordProcess.Header(
                    new WordProcess.Paragraph(
                        new WordProcess.ParagraphProperties(
                            new WordProcess.ParagraphStyleId() { Val = "Header" },
                            new WordProcess.Justification() { Val = headerPos }),
                        new WordProcess.Run(
                            new WordProcess.Text(headerText))
                    ));
            header.Save(headrPart);
            foreach (WordProcess.SectionProperties sectProperties in docPart.Document.Descendants<WordProcess.SectionProperties>())
            {
                WordProcess.HeaderReference newHeaderRef = new WordProcess.HeaderReference() { Id = docPart.GetIdOfPart(headrPart), Type = WordProcess.HeaderFooterValues.Default };
                sectProperties.Append(newHeaderRef);
            }
        }

        private static WordProcess.JustificationValues GetJustificationPosition(string strPosition)
        {
            WordProcess.JustificationValues emPosition = WordProcess.JustificationValues.Left;
            if (strPosition.ToLower().Equals("center", StringComparison.OrdinalIgnoreCase))
            {
                emPosition = WordProcess.JustificationValues.Center;
            }
            else if (strPosition.ToLower().Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                emPosition = WordProcess.JustificationValues.Right;
            }
            return emPosition;
        }

        private static string SetHeaderText(string text, string value, ref bool bExisted)
        {
            string tagText = text;
            //Separate the string using " ";
            List<string> tagTextList = new List<string>();
            if (!String.IsNullOrEmpty(tagText))
            {
                tagTextList = text.Split(new string[] { " ", "\r", "\n" }, StringSplitOptions.None).ToList<string>();
            }

            for (int i = 0; i < tagTextList.Count; i++)
            {
                string cellText = tagTextList[i];
                if (!String.IsNullOrEmpty(cellText))
                {
                    if (cellText.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        bExisted = true;
                        break;
                    }
                    else
                    {
                        int ind = value.IndexOf("__");
                        if (-1 != ind)
                        {
                            string keyColumn = value.Substring(0, ind + 2);
                            if (cellText.StartsWith(keyColumn, StringComparison.OrdinalIgnoreCase))
                            {
                                bExisted = true;
                                tagTextList[i] = value; // Replace the column value when existed same column.
                            }
                        }
                    }
                }
            }

            if (tagTextList.Count > 0)
            {
                tagText = string.Join(" ", tagTextList.ToArray()); //Join the string using " ";
            }
            return tagText;
        }

        private static Core.MsoTextEffectAlignment GetHeaderPosForPPT2003(string strPosition)
        {
            Core.MsoTextEffectAlignment alignment = Core.MsoTextEffectAlignment.msoTextEffectAlignmentCentered;
            if (strPosition.Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                alignment = Core.MsoTextEffectAlignment.msoTextEffectAlignmentLeft;
            }
            else if (strPosition.Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                alignment = Core.MsoTextEffectAlignment.msoTextEffectAlignmentRight;
            }
            return alignment;
        }




        private static void GetExcelThreePosHeader(string allHeader, ref string leftHeader, ref string centerHeader, ref string rightHeader)
        {
            if (string.IsNullOrEmpty(allHeader))
            {
                leftHeader = "";
                centerHeader = "";
                rightHeader = "";
            }
            else
            {
                if (!allHeader.Contains("&C") && !allHeader.Contains("&L") && !allHeader.Contains("&R"))
                {
                    leftHeader = "";
                    centerHeader = allHeader;
                    rightHeader = "";
                }
                else
                {
                    int pos = allHeader.IndexOf("&R");
                    if (-1 != pos)
                    {
                        rightHeader = allHeader.Substring(pos + 2);
                        leftHeader = allHeader.Substring(0, pos);
                        pos = leftHeader.IndexOf("&C");
                        if (-1 != pos)
                        {
                            centerHeader = leftHeader.Substring(pos + 2);
                            if (leftHeader.StartsWith("&L"))
                                leftHeader = leftHeader.Substring(2, pos - 2);
                        }
                        else if (leftHeader.StartsWith("&L"))
                        {
                            leftHeader = leftHeader.Substring(2);
                        }
                    }
                    else
                    {
                        pos = allHeader.IndexOf("&C");
                        if (-1 != pos)
                        {
                            centerHeader = allHeader.Substring(pos + 2);
                            if (allHeader.StartsWith("&L"))
                                leftHeader = allHeader.Substring(2, pos - 2);
                        }
                        else if (allHeader.StartsWith("&L"))
                        {
                            leftHeader = allHeader.Substring(2);
                        }
                    }
                }
            }
        }


        private static void HandleExcelAllLeftHeader(Excel.Worksheet workSheet, List<string> values)
        {
            string leftHeader = workSheet.PageSetup.LeftHeader;
            string centerHeader = workSheet.PageSetup.CenterHeader;
            string rightHeader = workSheet.PageSetup.RightHeader;
            MergeHeaderToTarget(ref leftHeader, ref centerHeader, values);
            MergeHeaderToTarget(ref leftHeader, ref rightHeader, values);
            HandleHeaderText(ref leftHeader, values);
            int length = 253 - centerHeader.Length - rightHeader.Length;
            workSheet.PageSetup.LeftHeader = leftHeader.Length <= length ? leftHeader : leftHeader.Substring(0, length);
            workSheet.PageSetup.CenterHeader = centerHeader;
            workSheet.PageSetup.RightHeader = rightHeader;

            leftHeader = workSheet.PageSetup.FirstPage.LeftHeader.Text;
            centerHeader = workSheet.PageSetup.FirstPage.CenterHeader.Text;
            rightHeader = workSheet.PageSetup.FirstPage.RightHeader.Text;
            MergeHeaderToTarget(ref leftHeader, ref centerHeader, values);
            MergeHeaderToTarget(ref leftHeader, ref rightHeader, values);
            length = 253 - centerHeader.Length - rightHeader.Length;
            HandleHeaderText(ref leftHeader, values);
            workSheet.PageSetup.FirstPage.LeftHeader.Text = leftHeader.Length <= length ? leftHeader : leftHeader.Substring(0, length);
            workSheet.PageSetup.FirstPage.CenterHeader.Text = centerHeader;
            workSheet.PageSetup.FirstPage.RightHeader.Text = rightHeader;

            leftHeader = workSheet.PageSetup.EvenPage.LeftHeader.Text;
            centerHeader = workSheet.PageSetup.EvenPage.CenterHeader.Text;
            rightHeader = workSheet.PageSetup.EvenPage.RightHeader.Text;
            MergeHeaderToTarget(ref leftHeader, ref centerHeader, values);
            MergeHeaderToTarget(ref leftHeader, ref rightHeader, values);
            length = 253 - centerHeader.Length - rightHeader.Length;
            HandleHeaderText(ref leftHeader, values);
            workSheet.PageSetup.EvenPage.LeftHeader.Text = leftHeader.Length <= length ? leftHeader : leftHeader.Substring(0, length);
            workSheet.PageSetup.EvenPage.CenterHeader.Text = centerHeader;
            workSheet.PageSetup.EvenPage.RightHeader.Text = rightHeader;
        }

        private static void HandleExcelAllCenterHeader(Excel.Worksheet workSheet, List<string> values)
        {
            string centerHeader = workSheet.PageSetup.CenterHeader;
            string header1 = workSheet.PageSetup.LeftHeader;
            string header2 = workSheet.PageSetup.RightHeader;
            MergeHeaderToTarget(ref centerHeader, ref header1, values);
            MergeHeaderToTarget(ref centerHeader, ref header2, values);
            int length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref centerHeader, values);
            workSheet.PageSetup.CenterHeader = centerHeader.Length <= length ? centerHeader : centerHeader.Substring(0, length);
            workSheet.PageSetup.LeftHeader = header1;
            workSheet.PageSetup.RightHeader = header2;

            centerHeader = workSheet.PageSetup.FirstPage.CenterHeader.Text;
            header1 = workSheet.PageSetup.FirstPage.LeftHeader.Text;
            header2 = workSheet.PageSetup.FirstPage.RightHeader.Text;
            MergeHeaderToTarget(ref centerHeader, ref header1, values);
            MergeHeaderToTarget(ref centerHeader, ref header2, values);
            length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref centerHeader, values);
            workSheet.PageSetup.FirstPage.CenterHeader.Text = centerHeader.Length <= length ? centerHeader : centerHeader.Substring(0, length);
            workSheet.PageSetup.FirstPage.LeftHeader.Text = header1;
            workSheet.PageSetup.FirstPage.RightHeader.Text = header2;

            centerHeader = workSheet.PageSetup.EvenPage.CenterHeader.Text;
            header1 = workSheet.PageSetup.EvenPage.LeftHeader.Text;
            header2 = workSheet.PageSetup.EvenPage.RightHeader.Text;
            MergeHeaderToTarget(ref centerHeader, ref header1, values);
            MergeHeaderToTarget(ref centerHeader, ref header2, values);
            length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref centerHeader, values);
            workSheet.PageSetup.EvenPage.CenterHeader.Text = centerHeader.Length <= length ? centerHeader : centerHeader.Substring(0, length);
            workSheet.PageSetup.EvenPage.LeftHeader.Text = header1;
            workSheet.PageSetup.EvenPage.RightHeader.Text = header2;
        }

        private static void HandleExcelAllRightHeader(Excel.Worksheet workSheet, List<string> values)
        {
            string targetHeader = workSheet.PageSetup.RightHeader;
            string header1 = workSheet.PageSetup.CenterHeader;
            string header2 = workSheet.PageSetup.LeftHeader;
            MergeHeaderToTarget(ref targetHeader, ref header1, values);
            MergeHeaderToTarget(ref targetHeader, ref header2, values);
            int length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref targetHeader, values);
            workSheet.PageSetup.RightHeader = targetHeader.Length <= length ? targetHeader : targetHeader.Substring(0, length);
            workSheet.PageSetup.CenterHeader = header1;
            workSheet.PageSetup.LeftHeader = header2;

            targetHeader = workSheet.PageSetup.FirstPage.RightHeader.Text;
            header1 = workSheet.PageSetup.FirstPage.CenterHeader.Text;
            header2 = workSheet.PageSetup.FirstPage.LeftHeader.Text;
            MergeHeaderToTarget(ref targetHeader, ref header1, values);
            MergeHeaderToTarget(ref targetHeader, ref header2, values);
            length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref targetHeader, values);
            workSheet.PageSetup.FirstPage.RightHeader.Text = targetHeader.Length <= length ? targetHeader : targetHeader.Substring(0, length);
            workSheet.PageSetup.FirstPage.CenterHeader.Text = header1;
            workSheet.PageSetup.FirstPage.LeftHeader.Text = header2;

            targetHeader = workSheet.PageSetup.EvenPage.RightHeader.Text;
            header1 = workSheet.PageSetup.EvenPage.CenterHeader.Text;
            header2 = workSheet.PageSetup.EvenPage.LeftHeader.Text;
            MergeHeaderToTarget(ref targetHeader, ref header1, values);
            MergeHeaderToTarget(ref targetHeader, ref header2, values);
            length = 253 - header1.Length - header2.Length;
            HandleHeaderText(ref targetHeader, values);
            workSheet.PageSetup.EvenPage.RightHeader.Text = targetHeader.Length <= length ? targetHeader : targetHeader.Substring(0, length);
            workSheet.PageSetup.EvenPage.CenterHeader.Text = header1;
            workSheet.PageSetup.EvenPage.LeftHeader.Text = header2;
        }

        private static void MergeHeaderToTarget(ref string strTargetHeaderText, ref string strheaderText, List<string> values)
        {
            bool bMerge = false;
            if (!string.IsNullOrEmpty(strheaderText))
            {
                foreach (string value in values)
                {
                    bool bExisted = false;
                    string header1 = strheaderText;
                    SetHeaderText(header1, value, ref bExisted);
                    if (bExisted)
                    {
                        bMerge = true;
                        break;
                    }
                }
                if (bMerge)
                {
                    strTargetHeaderText += string.IsNullOrEmpty(strTargetHeaderText) ? "" : "\n" + strheaderText;
                    strheaderText = "";
                }
                //      bMerge = false;
            }
            //   return bMerge;
        }

        private static void HandleHeaderText(ref string pageTargetHeader, List<string> values) // method to 2 method
        {
            if (string.IsNullOrEmpty(pageTargetHeader))
            {
                pageTargetHeader = "";
                foreach (string value in values)
                {
                    bool bExisted = false;
                    pageTargetHeader = SetHeaderText(pageTargetHeader, value, ref bExisted);
                    if (!bExisted)
                    {
                        pageTargetHeader = pageTargetHeader + " " + value;
                    }
                }
            }
            else
            {
                // string targetHeaderText = pageTargetHeader;
                foreach (string value in values)
                {
                    bool bExisted = false;
                    List<string> paragraphList = new List<string>();
                    paragraphList = pageTargetHeader.Split(new string[] { "\r", "\n" }, StringSplitOptions.None).ToList<string>();
                    for (int i = 0; i < paragraphList.Count(); i++)
                    {
                        paragraphList[i] = SetHeaderText(paragraphList[i], value, ref bExisted);
                        //   lastParagraph = paragraph;
                    }
                    if (!bExisted)
                    {
                        // targetHeaderText = targetHeaderText + " " + value;
                        paragraphList[paragraphList.Count() - 1] += " " + value;
                    }
                    pageTargetHeader = string.Join("\n", paragraphList.ToArray());
                }
            }
        }


        private static Dictionary<int, object> GetAllHeaderDic(Spreadsheet.HeaderFooter headerfooter)
        {
            Dictionary<int, object> headerDic = new Dictionary<int, object>();
            if (headerfooter.FirstHeader == null)
            {
                headerfooter.FirstHeader = new Spreadsheet.FirstHeader();
            }
            headerDic.Add(1, headerfooter.FirstHeader);
            if (headerfooter.EvenHeader == null)
            {
                headerfooter.EvenHeader = new Spreadsheet.EvenHeader();
            }
            headerDic.Add(2, headerfooter.EvenHeader);
            if (headerfooter.OddHeader == null)
            {
                headerfooter.OddHeader = new Spreadsheet.OddHeader();
            }
            headerDic.Add(3, headerfooter.OddHeader);
            return headerDic;
        }

        private static string GetHeaderText(KeyValuePair<int, object> kvp)
        {
            string headerText = "";
            switch (kvp.Key)
            {
                case 1:
                    headerText = ((Spreadsheet.FirstHeader)kvp.Value).Text;
                    break;
                case 2:
                    headerText = ((Spreadsheet.EvenHeader)kvp.Value).Text;
                    break;
                case 3:
                    headerText = ((Spreadsheet.OddHeader)kvp.Value).Text;
                    break;
            }
            return headerText;
        }

        private static void SetAllHeaderText(KeyValuePair<int, object> kvp, string headerText)
        {
            switch (kvp.Key)
            {
                case 1:
                    ((Spreadsheet.FirstHeader)kvp.Value).Text = headerText;
                    break;
                case 2:
                    ((Spreadsheet.EvenHeader)kvp.Value).Text = headerText;
                    break;
                case 3:
                    ((Spreadsheet.OddHeader)kvp.Value).Text = headerText;
                    break;
            }
        }

        private static string GetExcelHeaderPositionFlag(string strPosition)
        {
            string strFlag = "&C";
            if (strPosition.ToLower().Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                strFlag = "&L";
            }
            else if (strPosition.ToLower().Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                strFlag = "&R";
            }
            return strFlag;
        }


        private static void SetPPT2007FooterText(Presentation.ShapeTree tree, string position, List<string> values)
        {
            bool bExistsftr = false;
            foreach (Presentation.Shape shape in tree.Elements<Presentation.Shape>())
            {
                Presentation.NonVisualShapeProperties nvSpPr = shape.NonVisualShapeProperties;
                if (nvSpPr != null && nvSpPr.ApplicationNonVisualDrawingProperties != null)
                {
                    Presentation.ApplicationNonVisualDrawingProperties nvDrawProp = nvSpPr.ApplicationNonVisualDrawingProperties;
                    foreach (Presentation.PlaceholderShape phShape in nvDrawProp.Elements<Presentation.PlaceholderShape>())
                    {
                        if (phShape.Type != null && phShape.Type.Value.Equals(Presentation.PlaceholderValues.Footer))
                        {
                            bExistsftr = true;
                            foreach (string value in values)
                            {
                                bool bExisted = false;
                                Draw.Paragraph lastParagraph = null;
                                Presentation.TextBody textBody = shape.TextBody;
                                string oldFooterValue = "";
                                foreach (Draw.Paragraph paragraph in textBody.Elements<Draw.Paragraph>())
                                {
                                    oldFooterValue = paragraph.InnerText;
                                    string newFooterValue = SetHeaderText(oldFooterValue, value, ref bExisted);
                                    if (!newFooterValue.Equals(oldFooterValue))
                                    {
                                        paragraph.InnerXml = new Draw.Paragraph(
                                            GetParagraphPosition(position), new Draw.Run(new Draw.Text() { Text = newFooterValue })).InnerXml;
                                    }
                                    else
                                    {
                                        Draw.ParagraphProperties props = paragraph.ParagraphProperties;
                                        if (props == null)
                                        {
                                            props = new Draw.ParagraphProperties();
                                            paragraph.ParagraphProperties = props;
                                        }
                                        props.Alignment = GetParagraphPosition(position).Alignment;
                                    }
                                    lastParagraph = paragraph;
                                }
                                if (!bExisted)
                                {
                                    if (lastParagraph != null)
                                    {
                                        Draw.ParagraphProperties props = lastParagraph.ParagraphProperties;
                                        props.Alignment = GetParagraphPosition(position).Alignment;
                                        lastParagraph.InnerXml = new Draw.Paragraph(
                                                GetParagraphPosition(position), new Draw.Run(new Draw.Text() { Text = oldFooterValue + " " + value })).InnerXml;
                                    }
                                    else
                                    {
                                        textBody.Append(new Draw.Paragraph(GetParagraphPosition(position), new Draw.Run(new Draw.Text() { Text = value })));
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            if (!bExistsftr)
            {
                string headerFooterText = "";
                foreach (string value in values)
                {
                    bool bExisted = false;
                    headerFooterText = SetHeaderText(headerFooterText, value, ref bExisted);
                    if (!bExisted)
                    {
                        headerFooterText = headerFooterText + " " + value;
                    }
                }
                tree.AppendChild<Presentation.Shape>(CreateFooterShape(position, headerFooterText));
            }
        }

        private static Presentation.Shape CreateFooterShape(string textPosition, string footerText)
        {
            Presentation.PlaceholderShape phFooterShape = new Presentation.PlaceholderShape();
            phFooterShape.Type = Presentation.PlaceholderValues.Footer;
            phFooterShape.Size = Presentation.PlaceholderSizeValues.Quarter;
            phFooterShape.Index = (OpenXml.UInt32Value)11U;
            Presentation.Shape footerShape = new Presentation.Shape(
                    new Presentation.NonVisualShapeProperties(
                    new Presentation.NonVisualDrawingProperties() { Id = (OpenXml.UInt32Value)4U, Name = "Footer Placeholder 3" },
                    new Presentation.NonVisualShapeDrawingProperties(new Draw.ShapeLocks() { NoGrouping = true }),
                    new Presentation.ApplicationNonVisualDrawingProperties(phFooterShape)),
                    new Presentation.ShapeProperties(),
                    new Presentation.TextBody(
                        new Draw.BodyProperties(),
                        new Draw.ListStyle(),
                        new Draw.Paragraph(
                            GetParagraphPosition(textPosition),
                            new Draw.Run(
                            new Draw.Text() { Text = footerText }
                ))));
            return footerShape;
        }

        private static Draw.ParagraphProperties GetParagraphPosition(string strPosition)
        {
            Draw.ParagraphProperties paragraphProperties = new Draw.ParagraphProperties()
            {
                Alignment = Draw.TextAlignmentTypeValues.Left
            };
            if (strPosition.ToLower().Equals("center", StringComparison.OrdinalIgnoreCase))
            {
                paragraphProperties.Alignment = Draw.TextAlignmentTypeValues.Center;
            }
            else if (strPosition.ToLower().Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                paragraphProperties.Alignment = Draw.TextAlignmentTypeValues.Right;
            }
            return paragraphProperties;
        }
    }
}
