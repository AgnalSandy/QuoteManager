using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Invoices
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class PreviewModel : InvoicePageModelBase
    {
        public string CurrentTemplate { get; set; } = "Professional";

        public void OnGet(string? template)
        {
            // Set the template to preview (default to Professional)
            CurrentTemplate = template ?? "Professional";

            // Create dummy company data
            Company = new CompanySettingsViewModel
            {
                CompanyName = "Your Company Name",
                AddressLine1 = "123 Business Street",
                AddressLine2 = "Suite 100",
                City = "Mumbai",
                State = "Maharashtra",
                Country = "India",
                PinCode = "400001",
                PhoneNumber = "+91 98765 43210",
                Email = "info@yourcompany.com",
                Website = "www.yourcompany.com",
                GSTNumber = "27AABCU9603R1ZM",
                PANNumber = "AABCU9603R",
                LogoPath = null // Can be set if you have a default logo
            };

            // Create dummy invoice data
            Invoice = new InvoiceDetailsViewModel
            {
                Id = 1,
                InvoiceNumber = "INV-2026-0001",
                InvoiceDate = new DateTime(2026, 2, 28),
                DueDate = new DateTime(2026, 3, 15),
                Status = "Unpaid",
                TemplateType = CurrentTemplate,
                ClientName = "Sample Client Pvt Ltd",
                ClientEmail = "client@example.com",
                ClientPhone = "+91 98765 00000",
                QuoteTitle = "Website Development & Digital Marketing Services",
                QuoteNumber = "QT-2026-0042",
                SubTotal = 75000.00m,
                VATTotal = 13500.00m,
                Discount = 0.00m,
                GrandTotal = 88500.00m,
                BankName = "HDFC Bank",
                AccountName = "Your Company Name",
                SwiftAddress = "HDFCINBB",
                AccountNumber = "50200012345678",
                IBANNumber = "IN47HDFC0000502",
                Notes = "Payment is due within 15 days. Please make cheques payable to Your Company Name.",
                Items = new List<InvoiceItemViewModel>
                {
                    new InvoiceItemViewModel
                    {
                        ServiceName = "Website Development",
                        Description = "Complete responsive website with CMS integration",
                        Quantity = 1,
                        UnitPrice = 50000.00m,
                        Amount = 50000.00m,
                        Taxes = new List<InvoiceTaxItemViewModel>
                        {
                            new InvoiceTaxItemViewModel
                            {
                                TaxName = "GST",
                                TaxPercentage = 18.00m,
                                TaxAmount = 9000.00m
                            }
                        }
                    },
                    new InvoiceItemViewModel
                    {
                        ServiceName = "Digital Marketing Package",
                        Description = "SEO, Social Media Management, and Content Marketing for 3 months",
                        Quantity = 1,
                        UnitPrice = 25000.00m,
                        Amount = 25000.00m,
                        Taxes = new List<InvoiceTaxItemViewModel>
                        {
                            new InvoiceTaxItemViewModel
                            {
                                TaxName = "GST",
                                TaxPercentage = 18.00m,
                                TaxAmount = 4500.00m
                            }
                        }
                    }
                }
            };
        }
    }
}
