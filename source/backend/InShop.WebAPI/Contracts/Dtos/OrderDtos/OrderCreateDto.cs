using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Contracts.Dtos.OrderDtos
{
    public class CreateOrderDto
    {

        [Required(ErrorMessage = "Shipping method is required")]
        [StringLength(50)]
        public string ShipMethod { get; set; } = null!;

        [StringLength(200)]
        public string? ShipAddres { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50)]
        public string PayMethod { get; set; } = null!;

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100)]
        public string CustomerFullname { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = null!;

        [Required(ErrorMessage = "Phone is required")]
        [Phone]
        [StringLength(20)]
        public string CustomerPhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "At least one order item is required")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new();
    }
}

    
