using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.Validators;

public class CreateOrderRequestValidator : IRequestValidator<CreateOrderRequest>
{
    public void Validate(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerOrderId))
        {
            throw new InvalidOperationException("PartnerOrderId is required.");
        }

        ValidateCustomer(request.Customer);
        ValidateItems(request.Items);

        if (request.DeliveryMethod == DeliveryMethod.Shipping && request.ShippingAddress == null)
        {
            throw new InvalidOperationException("A shipping address is required when delivery method is Shipping.");
        }

        if (request.ShippingAddress != null)
        {
            ValidateShippingAddress(request.ShippingAddress);
        }
    }

    private static void ValidateCustomer(CustomerDto customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FirstName))
        {
            throw new InvalidOperationException("Customer first name is required.");
        }

        if (string.IsNullOrWhiteSpace(customer.LastName))
        {
            throw new InvalidOperationException("Customer last name is required.");
        }

        if (string.IsNullOrWhiteSpace(customer.Email) || !customer.Email.Contains('@'))
        {
            throw new InvalidOperationException("A valid customer email address is required.");
        }
    }

    private static void ValidateItems(List<OrderItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.PartnerSku))
            {
                throw new InvalidOperationException("Each order item must have a PartnerSku.");
            }

            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException($"Invalid quantity for '{item.PartnerSku}': quantity must be greater than zero.");
            }
        }

        var duplicateSku = items
            .GroupBy(i => i.PartnerSku, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSku != null)
        {
            throw new InvalidOperationException($"Duplicate PartnerSku '{duplicateSku.Key}' in order items. Combine quantities into a single line item.");
        }
    }

    private static void ValidateShippingAddress(ShippingAddressDto address)
    {
        if (string.IsNullOrWhiteSpace(address.Address1))
        {
            throw new InvalidOperationException("Shipping address line 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            throw new InvalidOperationException("Shipping city is required.");
        }

        if (string.IsNullOrWhiteSpace(address.Country) || address.Country.Trim().Length != 2)
        {
            throw new InvalidOperationException("Shipping country must be a 2-letter ISO country code (e.g. AU, US, GB).");
        }

        if (string.IsNullOrWhiteSpace(address.Zip))
        {
            throw new InvalidOperationException("Shipping postal code is required.");
        }
    }
}
