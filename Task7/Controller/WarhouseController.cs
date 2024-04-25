using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Task7.Models;

namespace Task7.Controller;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WarehouseController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("addProductToWarehouse")]
    public async Task<IActionResult> AddProductToWarehouse(
        [FromQuery] int idWarehouse,
        [FromQuery] int idProduct,
        [FromQuery] int idOrder,
        [FromQuery] int amount,
        [FromQuery] DateTime createdAt)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();


            var productExists = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT COUNT(1) FROM Products WHERE IdProduct = @IdProduct",
                new { IdProduct = idProduct });
            if (!productExists)
                return BadRequest("Product does not exist.");


            var warehouseExists = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT COUNT(1) FROM Warehouses WHERE IdWarehouse = @IdWarehouse",
                new { IdWarehouse = idWarehouse });
            if (!warehouseExists)
                return BadRequest("Warehouse does not exist.");


            var order = await connection.QueryFirstOrDefaultAsync(
                @"SELECT * FROM Orders 
                  WHERE IdOrder = @IdOrder AND IdProduct = @IdProduct AND Amount >= @Amount AND CreatedAt <= @CreatedAt",
                new { IdOrder = idOrder, IdProduct = idProduct, Amount = amount, CreatedAt = createdAt });
            if (order == null)
                return BadRequest("No valid order");


            var isFulfilled = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT COUNT(1) FROM Product_Warehouses WHERE IdOrder = @IdOrder",
                new { IdOrder = idOrder });
            if (isFulfilled)
                return BadRequest("fulfilled");


            var price = await connection.QueryFirstOrDefaultAsync<decimal>(
                "SELECT Price FROM Products WHERE IdProduct = @IdProduct",
                new { IdProduct = idProduct });

            var totalPrice = price * amount;
            var insertResult = await connection.ExecuteAsync(
                @"INSERT INTO Product_Warehouses (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                  VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @TotalPrice, @CurrentTime)",
                new
                {
                    IdWarehouse = idWarehouse, IdProduct = idProduct, IdOrder = idOrder, Amount = amount,
                    TotalPrice = totalPrice, CurrentTime = DateTime.UtcNow
                });

            if (insertResult < 1)
                return BadRequest("Failed");

            return Ok("Success");
        }
    }
    
    [HttpPost("addProductViaStoredProc")]
    public async Task<IActionResult> AddProductViaStoredProc([FromBody] ProductWarehouseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            var cmd = new SqlCommand("dbo.AddProductToWarehouse", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
            cmd.Parameters.AddWithValue("@IdOrder", dto.IdOrder);
            cmd.Parameters.AddWithValue("@Amount", dto.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                return Ok(new { IdProductWarehouse = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error");
            }
        }
    }
    public class ProductWarehouseDto
    {
        [Required]
        public int IdProduct { get; set; }

        [Required]
        public int IdWarehouse { get; set; }

        [Required]
        public int IdOrder { get; set; }

        [Required]
        public int Amount { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}