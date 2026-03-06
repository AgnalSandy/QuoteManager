# QuoteManager

A professional ASP.NET Core application for managing business quotes and invoices with role-based access control.

## Features

### Quote Management
- **Create Quotes**: Build detailed quotes with multiple line items, services, and taxes
- **Line Items**: Add multiple services with custom quantities and pricing
- **Tax Calculation**: Automatic tax calculation with support for multiple tax types per item
- **Quote Status**: Track quotes through their lifecycle (Pending → Accepted/Rejected)
- **Client Response**: Clients can accept or reject quotes directly

### Invoice Generation
- **Convert Quotes**: Generate professional invoices from accepted quotes
- **Multiple Templates**: Choose from Minimal, Modern, or Professional invoice templates
- **Payment Tracking**: Track invoice status (Unpaid, Paid, Overdue)

### Master Data Management
- **Service Master**: Maintain a catalog of services with standard rates
- **Tax Master**: Configure tax types and rates (GST, VAT, etc.)
- **Company Settings**: Customize company information for quotes and invoices

### User Management
- **Role-Based Access**: 
  - **SuperAdmin**: Full system access
  - **Admin**: Manage teams and their clients
  - **Staff**: Create quotes for assigned clients
  - **Client**: View and respond to quotes

## Workflow

1. **Setup Master Data** (Admin/SuperAdmin)
   - Configure Services in Service Master
   - Configure Taxes in Tax Master
   - Update Company Settings

2. **User Management** (Admin/SuperAdmin/Staff)
   - Create client accounts
   - Assign clients to staff members

3. **Quote Creation** (Admin/Staff)
   - Navigate to Quotes → Create New Quote
   - Select client and add quote details
   - Add line items with services and taxes
   - System calculates totals automatically
   - Submit to create quote

4. **Client Review** (Client)
   - View quotes assigned to them
   - Accept or Reject quotes

5. **Invoice Generation** (Admin)
   - For accepted quotes, click "Generate Invoice"
   - Select invoice template
   - Add payment details
   - Generate invoice

## Navigation

- **Dashboard**: Overview of statistics and quick access to all modules
  - Click on cards to navigate to Quotes, Invoices, or Users
- **Sidebar Menu**: Access all features
  - Dashboard
  - Quotes
  - Invoices
  - Master Data (Service Master, Tax Master)
  - Company Settings
  - Users

## Getting Started

1. Login with admin credentials
2. Setup master data (Services and Taxes)
3. Create client users
4. Start creating quotes!

## Technical Details

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Identity
- **UI**: Razor Pages with AdminLTE theme
- **Icons**: Font Awesome 6

## Quote Structure

Each quote contains:
- Header information (Client, Title, Description, Valid Until)
- Multiple line items, each with:
  - Service (from Service Master)
  - Quantity
  - Unit Price
  - Custom Description (optional)
  - Applied Taxes (multiple)
- Calculated totals (SubTotal, Total Tax, Grand Total)
