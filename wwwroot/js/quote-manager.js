// Modern, modular JavaScript for Quote Management
// Using ES6+ features with proper separation of concerns

class QuoteManager {
    constructor() {
        this.items = [];
        this.itemIndex = 0;
        this.servicePrices = {};
        this.taxRates = {};
        this.init();
    }

    init() {
        this.loadMasterData();
        this.attachEventListeners();
    }

    loadMasterData() {
        // Load service prices
        const serviceElements = document.querySelectorAll('[data-service-price]');
        serviceElements.forEach(el => {
            this.servicePrices[el.value] = parseFloat(el.dataset.servicePrice);
        });

        // Load tax rates
        const taxElements = document.querySelectorAll('[data-tax-rate]');
        taxElements.forEach(el => {
            this.taxRates[el.value] = parseFloat(el.dataset.taxRate);
        });
    }

    attachEventListeners() {
        const addItemBtn = document.getElementById('addItemBtn');
        if (addItemBtn) {
            addItemBtn.addEventListener('click', () => this.addItem());
        }

        // Form submission
        const form = document.getElementById('quoteForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleSubmit(e));
        }
    }

    addItem() {
        const template = document.getElementById('quoteItemTemplate');
        if (!template) return;

        const clone = template.content.cloneNode(true);
        const index = this.itemIndex++;

        // Replace INDEX placeholder
        clone.querySelectorAll('[name*="INDEX"]').forEach(el => {
            el.name = el.name.replace('INDEX', index);
            el.id = el.id?.replace('INDEX', index);
        });

        clone.querySelectorAll('[for*="INDEX"]').forEach(el => {
            el.setAttribute('for', el.getAttribute('for').replace('INDEX', index));
        });

        // Attach item-specific event listeners
        this.attachItemListeners(clone, index);

        const container = document.getElementById('quoteItemsContainer');
        container.appendChild(clone);

        this.items.push({ index, isCustom: false });
        this.calculateTotals();
    }

    attachItemListeners(element, index) {
        // Service selection change
        const serviceSelect = element.querySelector('.service-select');
        if (serviceSelect) {
            serviceSelect.addEventListener('change', (e) => this.handleServiceChange(e, index));
        }

        // Custom service toggle
        const customToggle = element.querySelector('.custom-service-checkbox');
        if (customToggle) {
            customToggle.addEventListener('change', (e) => this.toggleCustomService(e, index));
        }

        // Quantity/Price change
        const quantityInput = element.querySelector('[name*="Quantity"]');
        const priceInput = element.querySelector('[name*="UnitPrice"]');
        
        [quantityInput, priceInput].forEach(input => {
            if (input) {
                input.addEventListener('input', () => this.calculateItemAmount(index));
            }
        });

        // Tax selection
        const taxCheckboxes = element.querySelectorAll('.tax-checkbox');
        taxCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', () => this.calculateItemAmount(index));
        });

        // Remove button
        const removeBtn = element.querySelector('.remove-item-btn');
        if (removeBtn) {
            removeBtn.addEventListener('click', () => this.removeItem(index));
        }
    }

    handleServiceChange(event, index) {
        const serviceId = event.target.value;
        if (!serviceId) return;

        const price = this.servicePrices[serviceId] || 0;
        const priceInput = document.querySelector(`[name="Input.Items[${index}].UnitPrice"]`);
        
        if (priceInput) {
            priceInput.value = price.toFixed(2);
            this.calculateItemAmount(index);
        }
    }

    toggleCustomService(event, index) {
        const isCustom = event.target.checked;
        const itemRow = event.target.closest('.quote-item-row');
        
        const catalogRow = itemRow.querySelector('.service-catalog-row');
        const customRow = itemRow.querySelector('.custom-service-row');
        const serviceSelect = itemRow.querySelector('.service-select');
        const customNameInput = itemRow.querySelector('.custom-service-name');

        if (isCustom) {
            catalogRow.style.display = 'none';
            customRow.style.display = 'block';
            serviceSelect.required = false;
            customNameInput.required = true;
        } else {
            catalogRow.style.display = 'block';
            customRow.style.display = 'none';
            serviceSelect.required = true;
            customNameInput.required = false;
        }

        const item = this.items.find(i => i.index === index);
        if (item) {
            item.isCustom = isCustom;
        }
    }

    calculateItemAmount(index) {
        const quantityInput = document.querySelector(`[name="Input.Items[${index}].Quantity"]`);
        const priceInput = document.querySelector(`[name="Input.Items[${index}].UnitPrice"]`);
        const amountDisplay = document.getElementById(`itemAmount_${index}`);

        if (!quantityInput || !priceInput) return;

        const quantity = parseFloat(quantityInput.value) || 0;
        const price = parseFloat(priceInput.value) || 0;
        const amount = quantity * price;

        if (amountDisplay) {
            amountDisplay.textContent = `₹${this.formatCurrency(amount)}`;
        }

        this.calculateTotals();
    }

    calculateTotals() {
        let subTotal = 0;
        let totalTax = 0;

        this.items.forEach(item => {
            const { index } = item;
            const quantityInput = document.querySelector(`[name="Input.Items[${index}].Quantity"]`);
            const priceInput = document.querySelector(`[name="Input.Items[${index}].UnitPrice"]`);

            if (!quantityInput || !priceInput) return;

            const quantity = parseFloat(quantityInput.value) || 0;
            const price = parseFloat(priceInput.value) || 0;
            const itemAmount = quantity * price;

            subTotal += itemAmount;

            // Calculate taxes for this item
            const taxCheckboxes = document.querySelectorAll(`[name="Input.Items[${index}].SelectedTaxIds"]:checked`);
            taxCheckboxes.forEach(checkbox => {
                const taxRate = this.taxRates[checkbox.value] || 0;
                const taxAmount = (itemAmount * taxRate) / 100;
                totalTax += taxAmount;
            });
        });

        const grandTotal = subTotal + totalTax;

        // Update display
        this.updateTotalDisplay('subTotalDisplay', subTotal);
        this.updateTotalDisplay('totalTaxDisplay', totalTax);
        this.updateTotalDisplay('grandTotalDisplay', grandTotal);
    }

    updateTotalDisplay(elementId, amount) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = `₹${this.formatCurrency(amount)}`;
        }
    }

    formatCurrency(amount) {
        return amount.toLocaleString('en-IN', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    removeItem(index) {
        const itemRow = document.querySelector(`[data-item-index="${index}"]`);
        if (itemRow) {
            itemRow.remove();
        }

        this.items = this.items.filter(i => i.index !== index);
        this.calculateTotals();
    }

    handleSubmit(event) {
        // Validate at least one item
        if (this.items.length === 0) {
            event.preventDefault();
            this.showError('Please add at least one item to the quote');
            return false;
        }

        // Show loading state
        const submitBtn = event.target.querySelector('[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating Quote...';
        }

        return true;
    }

    showError(message) {
        // Use modern toast notification or alert
        const toast = document.createElement('div');
        toast.className = 'alert alert-danger alert-dismissible fade show position-fixed';
        toast.style.top = '20px';
        toast.style.right = '20px';
        toast.style.zIndex = '9999';
        toast.innerHTML = `
            ${message}
            <button type="button" class="close" data-dismiss="alert">
                <span>&times;</span>
            </button>
        `;
        document.body.appendChild(toast);

        setTimeout(() => toast.remove(), 5000);
    }
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.quoteManager = new QuoteManager();
    });
} else {
    window.quoteManager = new QuoteManager();
}

// Export for module usage
export default QuoteManager;
