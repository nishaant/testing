using Dapper;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using MySql.Data.MySqlClient;
using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    public class StockController : ApiController
    {
        private static ElasticClient elasticClient = ElasticClientInstance.GetInstance();
        private static MemcachedClient memClient = new MemcachedClient();
        // Api to Enter New Stock in the Database
        [HttpPost]
        public IHttpActionResult Post([FromBody]Stocks stockObj)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                String Error = "";
                if (!isValidUpdate(stockObj, ref Error))
                    return BadRequest(Error);
                if(!isValidInDB(stockObj))
                {
                    return BadRequest("Oops!!! Something Wrong has been Entered.....!!");
                }
                var par = new DynamicParameters();
                par = makeParameters(stockObj);           
                Db temp = new Db(); 
                IDbConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["myconn"].ConnectionString);
                var multi = connection.Query<Db>("table_insert", par, commandType: CommandType.StoredProcedure);
                {
                    temp = multi.First();
                }
                stockObj.stockId = temp.Id;
                
               ElasticSearchUpdate(stockObj);
                                
                return Ok("http://localhost:52227/api/Stock/stocks/" + temp.Id);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        

        //Delete API
        [Route("api/Stock/{id}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                IDbConnection connection= new MySqlConnection(ConfigurationManager.ConnectionStrings["myconn"].ConnectionString);
                Check obj = new Check();
                var par = new DynamicParameters();
                par.Add("par_id", id);
                try
                {
                    MemcachedClient client = new MemcachedClient();
                    client.Remove("memid" + id.ToString());

                }
                catch (Exception)
                {                    
                    throw;
                }
                var multi = connection.Query<Check>("check_exist", par, commandType: CommandType.StoredProcedure);
                {
                    obj = multi.First();
                }
                if (obj.exist == 0)
                    return BadRequest("The Stock Id You Entered Is Wrong!!!");
                else
                {
                    var index = elasticClient.Delete(elasticClient, i => i
                                    .Index("usedstock")
                                    .Type("usedcarstock")
                                    .Id("id" + id)
                                );
                    return Ok("Data Deleted Successfully"); 
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        //Api to update the stock entry
        [HttpPut, Route("api/Stock/{stockId}")]
        public IHttpActionResult Put(int stockId, [FromBody]Stocks stock)
        {
            try
            {
                if (!ModelState.IsValid) // CHECKING THE BODY CONTAINED 
                    return BadRequest(ModelState);
                String Error = "";
                if (isValidUpdate(stock,ref Error))
                {
                    if(!isValidInDB(stock))
                    {
                        return Ok("You are Entering Invalid Data......Not Present in the Database!!!");
                    }
                    if (updateDb(stock, stockId))
                        return Ok("Updated Successfully");
                    else
                        return BadRequest("This ID not found!!");
                }
                else
                {
                    return BadRequest(Error);
                }
            }
            catch (Exception e)
            {

                return InternalServerError(e);
            }
        }
        // CHECK WHEATHER THE NON-DATABASE PARAMETERS ARE CORRECTLY ENTERED OR NOT
        bool isValidUpdate(Stocks stock, ref String Error)
        {
            if (!(stock.price >= 10000 && stock.price <= 30000000))
            {
                Error = "Please Enter a valid price b/w 10000 and 3 Crore";
                return false;
            }

            if (!(stock.year >= 2010 && stock.year <= DateTime.Today.Year))
            {
                Error = "Please Enter a valid Date b/w 2010 and " + DateTime.Today.Year;
                return false;
            }

            if (!(stock.kilometer >= 100 && stock.kilometer <= 300000))
            {
                Error = "Please Enter a valid Kilometers b/w 100 and 3 Lakh";
                return false;
            }

            if (stock.fuelEconomy != -1 && !(stock.fuelEconomy >= 1 && stock.fuelEconomy <= 50))
            {
                Error = "Please Enter a valid Fuel Economy b/w 1 and 50 km";
                return false;
            }
            return true;
        }
        DynamicParameters makeCheckParameters(Stocks stock, int stockId = 0)
        {
            var par = new DynamicParameters();
            if (stockId != 0)
                par.Add("stock_id", stockId);
            par.Add("color_id", stock.color);
            par.Add("model_id", stock.model);
            par.Add("city_id", stock.city);
            par.Add("make_id", stock.make);
            par.Add("fuel_id", stock.fuelType);
            par.Add("version_id", stock.version);
            return par;
        }
        DynamicParameters makeParameters(Stocks stock, int stockId = 0)
        {
            var par = new DynamicParameters();
            if (stockId != 0)
                par.Add("stock_id", stockId);
            par.Add("price", stock.price);
            par.Add("yer", stock.year);
            par.Add("kilometer", stock.kilometer);
            par.Add("fuel_type", stock.fuelType);
            par.Add("model", stock.model);
            par.Add("city", stock.city);
            par.Add("color", stock.color);
            par.Add("fueleconomy", stock.fuelEconomy);
            par.Add("version", stock.version);
            par.Add("make", stock.make);
            return par;
        }
        //CHECK WHEATHER THE DATABASE CONTAIN THE ENTERED DATA OR NOT
        bool isValidInDB(Stocks stock)
        {
            var par = makeCheckParameters(stock);
            Entity Obj = new Entity();
            IDbConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["myconn"].ConnectionString);
            using (var multi = conn.QueryMultiple("verify", par, commandType: CommandType.StoredProcedure))
            {
                Obj = multi.Read<Entity>().First();
            }
            int check = Obj.color * Obj.model * Obj.fuel * Obj.city * Obj.make * Obj.version;
            if (check != 1)
                return false;
            return true;
        }
        // AFTER CHECKING ALL THE VALIDATION THIS FUNCTION UPDATE THE DATABASE WITH THE CHANGES
        bool updateDb(Stocks stock, int stockId)
        {
            var par = makeParameters(stock, stockId);
            Db temp = new Db();

            try
            {

                using (MemcachedClient mc = new MemcachedClient())
                {
                    var obj = mc.Get("memid" + stockId.ToString());
                    if (obj != null)
                        mc.Remove("memid" + stockId.ToString());
                    IDbConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["myconn"].ConnectionString);
                     var multi = connection.Query<Db>("table_update", par, commandType: CommandType.StoredProcedure);
                    {
                        temp = multi.First();
                    }

                }
                if (temp.Id == 1)
                {
                    stock.stockId = stockId;
                    ElasticSearchUpdate(stock);
                    return true;
                }

                else
                    return false;

            }
            catch (Exception)
            {

                throw;
            }
        }
        private void ElasticSearchUpdate(Stocks stockObj)
        {
            MemcachedClient memCacheClient = new MemcachedClient();
            UsedCarStock usedCarStock = new UsedCarStock();
            usedCarStock.StockId = stockObj.stockId;
            usedCarStock.FuelType = (String)memCacheClient.Get("usFuelType" + stockObj.fuelType.ToString());
            usedCarStock.City = (String)memCacheClient.Get("usCity" + stockObj.city.ToString());
            usedCarStock.Color = (String)memCacheClient.Get("usColor" + stockObj.color.ToString());
            usedCarStock.Make = (String)memCacheClient.Get("usMake" + stockObj.make.ToString());
            usedCarStock.Model = (String)memCacheClient.Get("usModel" + stockObj.model.ToString());
            usedCarStock.Version = (String)memCacheClient.Get("usVersion" + stockObj.version.ToString());
            usedCarStock.Price = stockObj.price;
            usedCarStock.Kilometer = stockObj.kilometer;
            usedCarStock.Year = stockObj.year;
            var index = elasticClient.Index(usedCarStock, i => i
                                .Index("usedstock")
                                .Type("usedcarstock")
                                .Id("id" + stockObj.stockId));
        }
    }
}
