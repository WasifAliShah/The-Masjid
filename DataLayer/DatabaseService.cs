using System.Data;

using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

public class donation
{
    public string head;

    public int amount_n;

    public int amount_c = 0;

    public string desc;

    public int donation_id;
    public bool isdel2 { get; set; } = false;
}


public class Student
{
    public string heading { get; set; }
    public string location { get; set; }
    public string description { get; set; }
    public string helping { get; set; }
    public int delid { get; set; }
    public bool isdel1 { get; set; } = false;

}

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Admin { get; set; }
    public bool isad { get; set; }
}

public class Users
{

    public string emai { get; set; }

    public string pas { get; set; }

    public string fnam{ get; set; }

    public string lnam { get; set; }

    public int userid { get; set; }

}

public class donated
{
    public int donated_id;

    public int donation_id;

    public int amount_do;

    public string fname1;

    public string heading;
    public int userid2 { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public int cId { get; set; }
    public DateTime OrderDate { get; set; }
    public bool isDeleted { get; set; }
}

public class ShippingDetails
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; }

    [Required(ErrorMessage = "City is required")]
    public string City { get; set; }

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; }

    [Required(ErrorMessage = "Zip Code is required")]
    public string ZipCode { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; }
    public bool isDeleted { get; set; }
}


public class Cart
{
    public int cId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool isDeleted { get; set; }
}

public class Products
{
    public int pId { get; set; }
    public string pName { get; set; }
    public string pDescription { get; set; }
    public int pPrice { get; set; }
    public string pImageURL { get; set; }
    public string Category { get; set; }
    public bool isDeleted { get; set; }
}


public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task AddProductAsync(Products product)
    {
        const string query = "INSERT INTO Products (pName, pDescription, pPrice, pImageURL, Category, isDeleted) VALUES (@pName, @pDescription, @pPrice, @pImageURL, @Category, @isDeleted)";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@pName", product.pName);
                command.Parameters.AddWithValue("@pDescription", product.pDescription);
                command.Parameters.AddWithValue("@pPrice", product.pPrice);
                command.Parameters.AddWithValue("@pImageURL", product.pImageURL);
                command.Parameters.AddWithValue("@Category", product.Category);
                command.Parameters.AddWithValue("@isDeleted", false);

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task DeleteProductAsync(int productId)
    {
        const string query = "UPDATE Products SET isDeleted = 1 WHERE pId = @pId";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@pId", productId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    public async Task PlaceOrderAsync(Order order, ShippingDetails shippingDetails)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    order.OrderDate = DateTime.Now;

                    // Insert order
                    var insertOrderCommand = new SqlCommand("INSERT INTO Orders (cId, OrderDate) OUTPUT INSERTED.OrderId VALUES (@cId, @OrderDate)", connection, transaction);
                    insertOrderCommand.Parameters.AddWithValue("@cId", order.cId);
                    insertOrderCommand.Parameters.AddWithValue("@OrderDate", order.OrderDate);

                    var orderId = (int)await insertOrderCommand.ExecuteScalarAsync();

                    // Insert shipping details
                    var insertShippingCommand = new SqlCommand("INSERT INTO ShippingDetails (OrderId, Name, Address, City, State, ZipCode, Email, Phone, PaymentMethod) VALUES (@OrderId, @Name, @Address, @City, @State, @ZipCode, @Email, @Phone, @PaymentMethod)", connection, transaction);
                    insertShippingCommand.Parameters.AddWithValue("@OrderId", orderId);
                    insertShippingCommand.Parameters.AddWithValue("@Name", shippingDetails.Name);
                    insertShippingCommand.Parameters.AddWithValue("@Address", shippingDetails.Address);
                    insertShippingCommand.Parameters.AddWithValue("@City", shippingDetails.City);
                    insertShippingCommand.Parameters.AddWithValue("@State", shippingDetails.State);
                    insertShippingCommand.Parameters.AddWithValue("@ZipCode", shippingDetails.ZipCode);
                    insertShippingCommand.Parameters.AddWithValue("@Email", shippingDetails.Email);
                    insertShippingCommand.Parameters.AddWithValue("@Phone", shippingDetails.Phone);
                    insertShippingCommand.Parameters.AddWithValue("@PaymentMethod", shippingDetails.PaymentMethod);

                    await insertShippingCommand.ExecuteNonQueryAsync();

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public async Task AddOrUpdateCartItemAsync(int productId, int quantity)
    {
        const string storedProcedure = "AddOrUpdateCartItem";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                command.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = quantity });

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task AddToCartAsync(int productId, int quantity)
    {
        const string storedProcedureCheck = "CheckCartItemExists";
        const string storedProcedureUpdate = "UpdateCartItemQuantity";
        const string storedProcedureAdd = "AddToCart";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Check if the item already exists in the cart and is not deleted
            using (var checkCommand = new SqlCommand(storedProcedureCheck, connection))
            {
                checkCommand.CommandType = CommandType.StoredProcedure;
                checkCommand.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });

                var exists = (int)(await checkCommand.ExecuteScalarAsync()) > 0;

                if (exists)
                {
                    // Update the quantity of the existing cart item
                    using (var updateCommand = new SqlCommand(storedProcedureUpdate, connection))
                    {
                        updateCommand.CommandType = CommandType.StoredProcedure;
                        updateCommand.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                        updateCommand.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = quantity });

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    // Add a new item to the cart
                    using (var addCommand = new SqlCommand(storedProcedureAdd, connection))
                    {
                        addCommand.CommandType = CommandType.StoredProcedure;
                        addCommand.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                        addCommand.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = quantity });

                        await addCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }

    public async Task<List<Products>> GetCartItemByProductIdAsync(int[] productIds)
    {
        var products = new List<Products>();

        if (productIds.Length == 0)
        {
            return products;
        }

        var productIdList = string.Join(",", productIds);

        const string storedProcedureName = "GetProductsByProductIds";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                // Add parameter for the stored procedure
                command.Parameters.AddWithValue("@ProductIds", productIdList);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var product = new Products
                        {
                            pId = reader.GetInt32(reader.GetOrdinal("pId")),
                            pPrice = reader.GetInt32(reader.GetOrdinal("pPrice")),
                            pName = reader.GetString(reader.GetOrdinal("pName")),
                            pImageURL = reader.GetString(reader.GetOrdinal("pImageURL")),
                            pDescription = reader.GetString(reader.GetOrdinal("pDescription")),
                            Category = reader.GetString(reader.GetOrdinal("Category"))
                        };
                        products.Add(product);
                    }
                }
            }
        }

        return products;
    }

    public async Task<List<Products>> GetProductsAsync()
    {
        var products = new List<Products>();
        const string storedProcedure = "GetProducts";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        products.Add(new Products
                        {
                            pId = reader.GetInt32(reader.GetOrdinal("pId")),
                            pName = reader.GetString(reader.GetOrdinal("pName")),
                            pDescription = reader.GetString(reader.GetOrdinal("pDescription")),
                            pPrice = reader.GetInt32(reader.GetOrdinal("pPrice")),
                            pImageURL = reader.GetString(reader.GetOrdinal("pImageURL")),
                            Category = reader.GetString(reader.GetOrdinal("Category"))
                        });
                    }
                }
            }
        }
        return products;
    }

    public async Task<List<Products>> GetRelatedProductsAsync(string category)
    {
        var products = new List<Products>();
        const string storedProcedure = "GetRelatedProducts";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Category", SqlDbType.NVarChar) { Value = category });

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        products.Add(new Products
                        {
                            pId = reader.GetInt32(reader.GetOrdinal("pId")),
                            pName = reader.GetString(reader.GetOrdinal("pName")),
                            pDescription = reader.GetString(reader.GetOrdinal("pDescription")),
                            pPrice = reader.GetInt32(reader.GetOrdinal("pPrice")),
                            pImageURL = reader.GetString(reader.GetOrdinal("pImageURL"))
                        });
                    }
                }
            }
        }
        return products;
    }

    public async Task<List<Products>> GetProductsByIdsAsync(int[] productIds)
    {
        var products = new List<Products>();

        if (productIds.Length == 0)
        {
            return products;
        }

        var productIdList = string.Join(",", productIds);

        const string storedProcedureName = "GetProductsByProductIds";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ProductIds", SqlDbType.NVarChar) { Value = productIdList });

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var product = new Products
                        {
                            pId = reader.GetInt32(reader.GetOrdinal("pId")),
                            pPrice = reader.GetInt32(reader.GetOrdinal("pPrice")),
                            pName = reader.GetString(reader.GetOrdinal("pName")),
                            pImageURL = reader.GetString(reader.GetOrdinal("pImageURL")),
                            pDescription = reader.GetString(reader.GetOrdinal("pDescription")),
                            Category = reader.GetString(reader.GetOrdinal("Category"))
                        };
                        products.Add(product);
                    }
                }
            }
        }

        return products;
    }

    public async Task UpdateCartItemQuantityAsync(int cartItemId, int quantity)
    {
        const string storedProcedure = "UpdateCartItemQuantity";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = cartItemId });
                command.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = quantity });

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task RemoveProductFromCartAsync(int productId)
    {
        const string storedProcedure = "RemoveProductFromCart";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
                command.Parameters.Add(new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = true });

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task ClearCartAsync()
    {
        const string storedProcedure = "ClearCart";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(storedProcedure, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = true });

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<List<Cart>> GetCartItemsAsync()
    {
        var cartItems = new List<Cart>();
        const string query = "SELECT * FROM Cart WHERE isDeleted = 0";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cartItems.Add(new Cart
                        {
                            cId = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            Quantity = reader.GetInt32(2)
                        });
                    }
                }
            }
        }
        return cartItems;
    }

    public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
    {
        DataTable result = new DataTable();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(result);
                }
            }
        }

        return result;
    }

    public async Task Adddonation(donation don)  // Asychronous method to insert data
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "INSERT INTO donations (heading, description, Amount_col, Amount_need, don_id, is_del) VALUES (@heading, @description, @Amount_col, @Amount_need, @don_id, @is_del)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Add parameters to prevent SQL injection
                command.Parameters.AddWithValue("@heading", don.head);
                command.Parameters.AddWithValue("@description", don.desc);
                command.Parameters.AddWithValue("@Amount_col", don.amount_c);
                command.Parameters.AddWithValue("@Amount_need", don.amount_n);
                command.Parameters.AddWithValue("@don_id", don.donation_id);
                command.Parameters.AddWithValue("@is_del", don.isdel2);
                await command.ExecuteNonQueryAsync();  // Execute the query
            }
        }
    }



    public async Task<List<donation>> GetAllDataAsync()
    {
        var donations = new List<donation>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string sql = "SELECT heading, description, Amount_col , Amount_need , don_id , is_del FROM donations";
            SqlCommand command = new SqlCommand(sql, connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    donations.Add(new donation
                    {
                        head = reader["heading"].ToString(),
                        desc = reader["description"].ToString(),
                        amount_c = reader.GetInt32(reader.GetOrdinal("Amount_col")),
                        amount_n = reader.GetInt32(reader.GetOrdinal("Amount_need")),
                        donation_id = reader.GetInt32(reader.GetOrdinal("don_id")),
                        isdel2 = reader.GetBoolean(reader.GetOrdinal("is_del"))
                });
                }
            }
        }

        return donations;
    }

    public async Task<List<Student>> GetAllDataAsync2()
    {
        var students = new List<Student>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string sql = "SELECT head, loc, dec , help , upid , isdel FROM admino";
            SqlCommand command = new SqlCommand(sql, connection);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    students.Add(new Student
                    {
                        heading = reader["head"].ToString(),
                        location = reader["loc"].ToString(),
                        description = reader["dec"].ToString(),
                        helping = reader["help"].ToString(),
                        delid = reader.GetInt32(reader.GetOrdinal("upid")),
                        isdel1 = reader.GetBoolean(reader.GetOrdinal("isdel"))
                });
                }
            }
        }

        return students;
    }

    public async Task AddStudent(Student student)  // Asynchronous method to insert data
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "INSERT INTO admino (head, loc, dec, help,isdel) VALUES (@head, @loc, @dec, @help, @isdel)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Add parameters to prevent SQL injection
                command.Parameters.AddWithValue("@head", student.heading);
                command.Parameters.AddWithValue("@loc", student.location);
                command.Parameters.AddWithValue("@dec", student.description);
                command.Parameters.AddWithValue("@help", student.helping);
                command.Parameters.AddWithValue("@isdel", student.isdel1);
               
                await command.ExecuteNonQueryAsync();  // Execute the query
            }
        }
    }

    public async Task AddUser(Users user)  // Asynchronous method to insert data
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "INSERT INTO [User] (Firstname, Lastname, password, email) VALUES (@Firstname, @Lastname, @password, @email)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Add parameters to prevent SQL injection
                command.Parameters.AddWithValue("@email", user.emai);
                command.Parameters.AddWithValue("@password", user.pas);
                command.Parameters.AddWithValue("@Firstname", user.fnam);
                command.Parameters.AddWithValue("@Lastname", user.lnam);
                await command.ExecuteNonQueryAsync();  // Execute the query
            }
        }
    }

    public async Task<(bool success, int userId)> CheckLogin(string email2, string password2, bool isad)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT UserId FROM [User] WHERE email = @email AND password = @password AND Admin = @Admin", connection);
            command.Parameters.AddWithValue("@email", email2);
            command.Parameters.AddWithValue("@password", password2);
            command.Parameters.AddWithValue("@Admin", isad);
            
            var result = await command.ExecuteScalarAsync();
            if(result !=null && result != DBNull.Value)
            {
                int userId = Convert.ToInt32(result);
                return(true, userId);

            }
          
                else
                {
                    return (false, -1); // Indicate unsuccessful login
                }
        }
        
    }

    public async Task UpdateDonationAsync(donation updatedDonation)
    {

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string sql = "UPDATE donations SET heading = @heading, Amount_need = @Amount_need, description = @description WHERE don_id = @don_id";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                // Add parameters
                command.Parameters.AddWithValue("@heading", updatedDonation.head);
                command.Parameters.AddWithValue("@amount_need", updatedDonation.amount_n);
                command.Parameters.AddWithValue("@description", updatedDonation.desc);
                command.Parameters.AddWithValue("@don_id", updatedDonation.donation_id);

                // Open connection
                await connection.OpenAsync();

                // Execute the update command
                int rowsAffected = await command.ExecuteNonQueryAsync();

                // Handle success or failure
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Donation updated successfully.");
                }
                else
                {
                    Console.WriteLine("No donation updated.");
                }
            }

        }
    }
    public async Task Update(int additionalAmount, int id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Retrieve the current amount collected
            var getCurrentAmountCommand = new SqlCommand("SELECT Amount_col FROM donations WHERE don_id = @don_id", connection);
            getCurrentAmountCommand.Parameters.AddWithValue("@don_id", id);
            int currentAmount = Convert.ToInt32(await getCurrentAmountCommand.ExecuteScalarAsync());

            // Calculate the new total amount
            int newTotalAmount = currentAmount + additionalAmount;

            // Update the database with the new total amount
            var updateCommand = new SqlCommand("UPDATE donations SET Amount_col = @Amount_col WHERE don_id = @don_id", connection);
            updateCommand.Parameters.AddWithValue("@Amount_col", newTotalAmount);
            updateCommand.Parameters.AddWithValue("@don_id", id);
            await updateCommand.ExecuteNonQueryAsync();
        }
    }

    public async Task Adddonate(donated done)  // Asynchronous method to insert data
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "INSERT INTO donate (don_id, amount_don, title, fname, UserId) VALUES (@don_id, @amount_don, @title, @fname, @UserId)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Add parameters to prevent SQL injection
                command.Parameters.AddWithValue("@UserId", done.userid2);
                command.Parameters.AddWithValue("@don_id", done.donation_id);
                command.Parameters.AddWithValue("@amount_don", done.amount_do);
                command.Parameters.AddWithValue("@title", done.heading);
                command.Parameters.AddWithValue("@fname", done.fname1);
                await command.ExecuteNonQueryAsync();  // Execute the query
            }
        }
    }

    public async Task<List<donated>> GetDonateListAsync()
    {
        var donateList = new List<donated>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            string query = "SELECT don_id, title, amount_don, fname FROM donate";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        donateList.Add(new donated
                        {
                            fname1 = reader["fname"].ToString(),
                            heading = reader["title"].ToString(),
                            amount_do = reader.GetInt32(reader.GetOrdinal("amount_don"))
                        });
                    }
                }
            }
        }

        return donateList;
    }

    public async Task<string> GetFirstnameByUserIdAsync(int userId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "SELECT Firstname FROM [User] WHERE userId = @userId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                object result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
        }
    }

    public async Task<string> GetHeadingByIdAsync(int donId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "SELECT heading FROM donations WHERE don_id = @don_id";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@don_id", donId);

                object result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
        }
    }

    public async Task DeleteupdateAsync(int delId)
    {
        const string query = "UPDATE admino SET isdel = 1 WHERE upId = @upId";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@upId", delId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task DeleteDonationAsync(int delId)
    {
        const string query = "UPDATE donations SET is_del = 1 WHERE don_id = @don_id";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@don_id", delId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

}