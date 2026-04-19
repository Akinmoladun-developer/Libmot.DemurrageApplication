using Libmot.DemurrageApplicationAPI.DTOs.Invoice;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Rectangle = iTextSharp.text.Rectangle;
using Font = iTextSharp.text.Font;
using Document = iTextSharp.text.Document;
using Paragraph = iTextSharp.text.Paragraph;



namespace Libmot.DemurrageApplicationAPI.Helpers
{
    public static class InvoicePdfGenerator
    {
        // ── Brand colours ─────────────────────────────────────────────────
        private static readonly BaseColor BrandYellow = new(255, 193, 7);
        private static readonly BaseColor BrandDark = new(33, 37, 41);
        private static readonly BaseColor TableHeader = new(52, 58, 64);
        private static readonly BaseColor RowAlt = new(248, 249, 250);
        private static readonly BaseColor BorderGray = new(222, 226, 230);
        private static readonly BaseColor RedAlert = new(220, 53, 69);
        private static readonly BaseColor GreenPaid = new(40, 167, 69);

        // ── Fonts ─────────────────────────────────────────────────────────
        private static readonly Font TitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BrandDark);
        private static readonly Font SubtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(169, 169, 169));
        private static readonly Font HeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(169, 169, 169));
        private static readonly Font BodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BrandDark);
        private static readonly Font BoldBody = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BrandDark);
        private static readonly Font SmallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(169, 169, 169));
        private static readonly Font TotalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BrandDark);
        private static readonly Font WatermarkFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 48, new BaseColor(0, 0, 0, 30));

        public static byte[] Generate(InvoiceResponseDto invoice)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 40, 40, 40, 40);
            var writer = PdfWriter.GetInstance(doc, ms);

            doc.Open();

            // ── Watermark for paid invoices ───────────────────────────────
            if (invoice.Status == "Paid")
            {
                var cb = writer.DirectContentUnder;
                var gs = new PdfGState { FillOpacity = 0.08f };
                cb.SetGState(gs);
                cb.BeginText();
                cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD,
                                  BaseFont.CP1252, false), 72);
                cb.SetColorFill(new BaseColor(40, 167, 69));
                cb.ShowTextAligned(Element.ALIGN_CENTER, "PAID",
                                   PageSize.A4.Width / 2, PageSize.A4.Height / 2, 45);
                cb.EndText();
            }

            // ── Header band ───────────────────────────────────────────────
            AddHeaderBand(doc, invoice);

            doc.Add(new Paragraph(" "));

            // ── Shipment & customer info grid ─────────────────────────────
            AddInfoGrid(doc, invoice);

            doc.Add(new Paragraph(" "));

            // ── Line items table ──────────────────────────────────────────
            AddLineItemsTable(doc, invoice);

            doc.Add(new Paragraph(" "));

            // ── Totals block ──────────────────────────────────────────────
            AddTotalsBlock(doc, invoice);

            doc.Add(new Paragraph(" "));

            // ── Payment history ───────────────────────────────────────────
            if (invoice.Payments.Any())
            {
                AddPaymentHistory(doc, invoice);
                doc.Add(new Paragraph(" "));
            }

            // ── Notes ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
                AddNotesBlock(doc, invoice.Notes);

            // ── Footer ────────────────────────────────────────────────────
            AddFooter(doc);

            doc.Close();
            return ms.ToArray();
        }

        // ── Header band ───────────────────────────────────────────────────
        private static void AddHeaderBand(Document doc, InvoiceResponseDto invoice)
        {
            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 60, 40 });

            // Left: company branding
            var leftCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 10
            };
            leftCell.AddElement(new Paragraph("LIBMOT EXPRESS", TitleFont));
            leftCell.AddElement(new Paragraph("118 Ago Palace Way, Okota-Isolo, Lagos, Nigeria", SubtitleFont));
            leftCell.AddElement(new Paragraph("support@libmotexpress.com  |  +234 906 254 7031", SubtitleFont));

            // Right: invoice identity
            var rightCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                PaddingBottom = 10
            };

            var statusColor = invoice.Status switch
            {
                "Paid" => GreenPaid,
                "Overdue" => RedAlert,
                "Disputed" => new BaseColor(255, 153, 0),
                _ => TableHeader
            };

            rightCell.AddElement(new Paragraph("DEMURRAGE INVOICE",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BrandDark)));
            rightCell.AddElement(new Paragraph(invoice.InvoiceNumber,
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BrandYellow)));
            rightCell.AddElement(new Paragraph($"Status: {invoice.Status.ToUpper()}",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, statusColor)));
            rightCell.AddElement(new Paragraph(
                $"Issued: {invoice.IssuedDate:dd MMM yyyy}", SmallFont));
            rightCell.AddElement(new Paragraph(
                $"Due:    {invoice.DueDate:dd MMM yyyy}", SmallFont));

            table.AddCell(leftCell);
            table.AddCell(rightCell);

            // Yellow divider line
            var divider = new PdfPTable(1) { WidthPercentage = 100 };
            var divCell = new PdfPCell(new Phrase(" "))
            {
                BackgroundColor = BrandYellow,
                FixedHeight = 4,
                Border = Rectangle.NO_BORDER
            };
            divider.AddCell(divCell);

            doc.Add(table);
            doc.Add(divider);
        }

        // ── Info grid ─────────────────────────────────────────────────────
        private static void AddInfoGrid(Document doc, InvoiceResponseDto invoice)
        {
            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 50, 50 });

            // Bill to
            var billTo = new PdfPCell
            {
                Border = Rectangle.BOX,
                BorderColor = BorderGray,
                Padding = 10
            };
            billTo.AddElement(new Paragraph("BILL TO", BoldBody));
            billTo.AddElement(new Paragraph(invoice.CustomerCompany, BoldBody));
            billTo.AddElement(new Paragraph(invoice.CustomerName, BodyFont));
            billTo.AddElement(new Paragraph(invoice.CustomerPhone, BodyFont));
            billTo.AddElement(new Paragraph(invoice.CustomerEmail, BodyFont));

            // Shipment details
            var shipInfo = new PdfPCell
            {
                Border = Rectangle.BOX,
                BorderColor = BorderGray,
                Padding = 10
            };
            shipInfo.AddElement(new Paragraph("SHIPMENT DETAILS", BoldBody));
            AddKeyValue(shipInfo, "Waybill", invoice.WaybillNumber);
            AddKeyValue(shipInfo, "Origin", invoice.OriginState);
            AddKeyValue(shipInfo, "Destination", invoice.DestinationState);
            AddKeyValue(shipInfo, "Item", invoice.ItemDescription);
            AddKeyValue(shipInfo, "Arrival Date", invoice.ArrivalDate.ToString("dd MMM yyyy"));
            AddKeyValue(shipInfo, "Free Days End", invoice.FreeDaysExpiry.ToString("dd MMM yyyy"));

            table.AddCell(billTo);
            table.AddCell(shipInfo);
            doc.Add(table);
        }

        // ── Line items table ──────────────────────────────────────────────
        private static void AddLineItemsTable(Document doc, InvoiceResponseDto invoice)
        {
            //var sectionLabel = new Paragraph("DEMURRAGE CHARGES BREAKDOWN", BoldBody)
            //{ SpacingBottom = 6 };

            var sectionLabel = new Paragraph("DEMURRAGE CHARGES BREAKDOWN", BoldBody);
            sectionLabel.SpacingAfter = 6;
            doc.Add(sectionLabel);

            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 20, 30, 15, 18, 17 });

            // Header row
            foreach (var h in new[] { "Tier", "Date Range", "Days", "Rate / Day (₦)", "Total (₦)" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, HeaderFont))
                {
                    BackgroundColor = TableHeader,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6,
                    Border = Rectangle.NO_BORDER
                });
            }

            // Data rows
            bool alt = false;
            foreach (var line in invoice.LineItems)
            {
                  var bg = alt ? RowAlt : new BaseColor(169, 169, 169);
                  alt = !alt;

                table.AddCell(DataCell(line.TierName, bg));
                table.AddCell(DataCell(line.DateRange, bg));
                table.AddCell(DataCell(line.Days.ToString(), bg, Element.ALIGN_CENTER));
                table.AddCell(DataCell($"{line.DailyRate:N2}", bg, Element.ALIGN_RIGHT));
                table.AddCell(DataCell($"{line.LineTotal:N2}", bg, Element.ALIGN_RIGHT));
            }

            doc.Add(table);
        }

        // ── Totals block ──────────────────────────────────────────────────
        private static void AddTotalsBlock(Document doc, InvoiceResponseDto invoice)
        {
            var outer = new PdfPTable(2) { WidthPercentage = 100 };
            outer.SetWidths(new float[] { 55, 45 });

            // Empty left side
            outer.AddCell(new PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER });

            // Right: totals
            var totals = new PdfPTable(2) { WidthPercentage = 100 };
            totals.SetWidths(new float[] { 55, 45 });

            AddTotalRow(totals, "Demurrage Days:", $"{invoice.DemurrageDays} days");
            AddTotalRow(totals, "Total Amount:", $"₦{invoice.TotalAmount:N2}", bold: true);
            AddTotalRow(totals, "Amount Paid:", $"₦{invoice.AmountPaid:N2}");

            // Balance
            var balColor = invoice.BalanceRemaining > 0 ? RedAlert : GreenPaid;
            var balFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, balColor);

            //var labelCell = new PdfPCell(new Phrase("Balance Due:", TotalFont))
            //{ Border = Rectangle.TOP, BorderColor = BrandYellow, Padding = 6 };
            //var valueCell = new PdfPCell(new Phrase($"₦{invoice.BalanceRemaining:N2}", balFont))
            //{
            //    Border = Rectangle.TOP,
            //    BorderColor = BrandYellow,
            //    Padding = 6,
            //    HorizontalAlignment = Element.ALIGN_RIGHT
            //};

            var labelCell = new PdfPCell(new Phrase("Balance Due:", TotalFont))
            { Border = 2, BorderColor = BrandYellow, Padding = 6 };  // 2 = TOP

            var valueCell = new PdfPCell(new Phrase($"₦{invoice.BalanceRemaining:N2}", balFont))
            {
                Border = 2,  // 2 = TOP
                BorderColor = BrandYellow,
                Padding = 6,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };

            totals.AddCell(labelCell);
            totals.AddCell(valueCell);

            var totalsCell = new PdfPCell(totals) { Border = Rectangle.NO_BORDER };
            outer.AddCell(totalsCell);
            doc.Add(outer);
        }

        // ── Payment history ───────────────────────────────────────────────
        private static void AddPaymentHistory(Document doc, InvoiceResponseDto invoice)
        {
            doc.Add(new Paragraph("PAYMENT HISTORY", BoldBody) { SpacingAfter = 6 });

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 15, 30, 30, 25 });

            foreach (var h in new[] { "#", "Date", "Method", "Amount (₦)" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, HeaderFont))
                {
                    BackgroundColor = TableHeader,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5,
                    Border = Rectangle.NO_BORDER
                });
            }

            int i = 1;
            foreach (var p in invoice.Payments)
            {
                //var bg = i % 2 == 0 ? RowAlt : BaseColor.WHITE;
                var bg = i % 2 == 0 ? RowAlt : new BaseColor(255, 255, 255);
                table.AddCell(DataCell(i.ToString(), bg, Element.ALIGN_CENTER));
                table.AddCell(DataCell(p.PaymentDate.ToString("dd MMM yyyy"), bg));
                table.AddCell(DataCell(p.PaymentMethod, bg));
                table.AddCell(DataCell($"{p.AmountPaid:N2}", bg, Element.ALIGN_RIGHT));
                i++;
            }

            doc.Add(table);
        }

        // ── Notes block ───────────────────────────────────────────────────
        private static void AddNotesBlock(Document doc, string notes)
        {
            //var cell = new PdfPCell
            //{
            //    Border = Rectangle.LEFT,
            //    //Border = iTextSharp.text.Rectangle.LEFT;
            //    BorderColor = BrandYellow,
            //    BorderWidth = 3,
            //    Padding = 10,
            //    BackgroundColor = RowAlt
            //};

            var cell = new PdfPCell
            {
                Border = 8,  // 8 = LEFT
                BorderColor = BrandYellow,
                BorderWidth = 3,
                Padding = 10,
                BackgroundColor = RowAlt
            };

            cell.AddElement(new Paragraph("NOTES", BoldBody));
            cell.AddElement(new Paragraph(notes, BodyFont));

            var t = new PdfPTable(1) { WidthPercentage = 100 };
            t.AddCell(cell);
            doc.Add(t);
        }

        // ── Footer ────────────────────────────────────────────────────────
        private static void AddFooter(Document doc)
        {
            doc.Add(new Paragraph(" "));
            var sep = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(
                0.5f, 100f, BorderGray, Element.ALIGN_CENTER, -2)));
            doc.Add(sep);

            var footer = new Paragraph(
                "This invoice was generated electronically by Libmot Express Demurrage System. " +
                "For disputes, contact support@libmotexpress.com within 48 hours of receipt.",
                SmallFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 6
            };
            doc.Add(footer);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static void AddKeyValue(PdfPCell cell, string key, string value)
        {
            var p = new Paragraph();
            p.Add(new Chunk($"{key}: ", BoldBody));
            p.Add(new Chunk(value, BodyFont));
            cell.AddElement(p);
        }

        //private static PdfPCell DataCell(
        //    string text, BaseColor bg, int align = Element.ALIGN_LEFT) =>
        //    new(new Phrase(text, BodyFont))
        //    {
        //        BackgroundColor = bg,
        //        HorizontalAlignment = align,
        //        Padding = 5,
        //        Border = Rectangle.BOTTOM,
        //        //Border = iTextSharp.text.Rectangle.BOTTOM;
        //        BorderColor = BorderGray
        //    };

        private static PdfPCell DataCell(
        string text, BaseColor bg, int align = Element.ALIGN_LEFT) =>
        new(new Phrase(text, BodyFont))
        {
            BackgroundColor = bg,
            HorizontalAlignment = align,
            Padding = 5,
            Border = 4,  // 4 = BOTTOM
            BorderColor = BorderGray
        };

        private static void AddTotalRow(
            PdfPTable table, string label, string value, bool bold = false)
        {
            var font = bold ? BoldBody : BodyFont;
            table.AddCell(new PdfPCell(new Phrase(label, font))
            { Border = Rectangle.NO_BORDER, Padding = 4 });
            table.AddCell(new PdfPCell(new Phrase(value, font))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 4,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });
        }
    }
}
