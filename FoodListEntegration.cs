

using Newtonsoft.Json;
using ProdaIntranetSource.Application.Extensions;
using psip_DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

public static class FoodService
{
    private static Root menu = new Root();
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
    public static Root GetFoodMenu(string city)
    {
        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("" + city),
                Method = HttpMethod.Get
            };
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                var serviceResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                menu = JsonConvert.DeserializeObject<Root>(serviceResponse.Replace("Main Food", "MainFood").Replace("Diet Food", "DietFood"));
                return menu;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return null;
            }
        }
    }

    public static string GetFoodHtml(Root menus)
    {
        int menuCalorie = 0;
        int dietMenuCalorie = 0;

        if (menus.MainFood.Any())
        {
            var mainMenu = menus.MainFood.FirstOrDefault();
            menuCalorie += mainMenu.SoupCalorie +
                           mainMenu.MainDishCalorie +
                           mainMenu.SideDishCalorie +
                           mainMenu.SoftDrinksCalorie +
                           mainMenu.DessertCalorie;

        }

        if (menus.DietFood.Any())
        {
            var dietMenu = menus.DietFood.FirstOrDefault();
            dietMenuCalorie += dietMenu.DietSoupCalorie ?? 0 +
                               dietMenu.DietMainDishCalorie ?? 0 +
                               dietMenu.FruitCalorie ?? 0 +
                               dietMenu.YogurtCalorie ?? 0;

        }

        var sb = new StringBuilder();
        sb.Append("<div width=\"280\" class=\"food-menu-table\">");

        //MAIN FOOD
        sb.Append("<div>");
        sb.Append("<p class=\"header\" style=\"font-size=16px;\">MENÜ (" + menuCalorie + " kalori)</p>");
        sb.Append("<ul style=\"background-color: #ececec; border-radius: 25px; margin-top: 10px;\">");
        foreach (var item in menus.MainFood)
        {
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.Soup + "</span><small>" + item.SoupCalorie + " kalori</small></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.MainDish + "</span><small>" + item.MainDishCalorie + " kalori</small></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.OptionalMainDish + "</span><small>" + item.OptionalMainDishCalorie + " kalori</small></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.Dessert + "</span><small>" + item.DessertCalorie + " kalori</small></li>");
        }

        sb.Append("</ul>");
        sb.Append("</div>");

        //DIET FOOD
        if (menus.DietFood.Any())
        {
            sb.Append("<div class=\"mt-4\">");
            sb.Append("<p class=\"header\" style=\"font-size=16px;margin-top:25px;\" >DİYET MENÜ (" + dietMenuCalorie + " kalori)</p>");
            sb.Append("<ul style=\"background-color: #F8F8F8; border-radius: 25px; margin-top: 10px;\">");
            foreach (var item in menus.DietFood)
            {
                sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.DietSoup + "</span><small>" + item.DietSoupCalorie + " kalori</small></li>");
                sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.DietMainDish + "</span><small>" + item.DietMainDishCalorie + " kalori</small></li>");
                sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.DietOptionalMainDish + "</span><small>" + item.DietOptionalMainDishCalorie + " kalori</small></li>");
                if (item.Fruit.HasValue && item.Fruit.Value)
                    sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">Meyve</span><small>" + item.FruitCalorie + " kalori</small></li>");
                if (item.Yogurt.HasValue && item.Yogurt.Value)
                    sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">Yoğurt</span><small>" + item.YogurtCalorie + " kalori</small></li>");
            }
            sb.Append("</ul>");
            sb.Append("</div>");
        }


        //SALADS
        sb.Append("<div class=\"mt-4\">");
        sb.Append("<p class=\"header\" style=\"font-size=16px;margin-top:25px;\" >SALATALAR </p>");
        sb.Append("<ul style=\"background-color: #F8F8F8; border-radius: 25px; margin-top: 10px;\">");
        foreach (var item in menus.Salad)
        {
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption1 + "</span></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption2 + "</span></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption3 + "</span></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption4 + "</span></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption5 + "</span></li>");
            sb.Append("<li style=\"display:flex\"><span style=\"font-size=13px;\">" + item.SaladOption6 + "</span></li>");
        }
        sb.Append("</ul>");
        sb.Append("</div>");

        sb.Append("</div>");

        return sb.ToString();

    }

    public static void WriteDb()
    {

        var vt = new psip_DBConnect(true);
       
        var cities = new Dictionary<string, string> { { "İstanbul", "istanbul" }, { "Ankara - HTK", "ankara-hkt" } };
        string spName = "psip_V1.msp_EventInsert";

        foreach (var city in cities)
        {
            var foodList = GetFoodMenu(city.Key);

            foreach (var food in foodList.MainFood.GroupBy(x => x.Date))
            {
                var foodDayStr = food.Key.ToString("yyyy-MM-dd");
                var seoUrl = city.Value.ToCustomSeoFriendly() + "-gunun-yemek-listesi-" + foodDayStr;
                //var date = "istanbul-yemek-listesi-" + food.Key.ToString("yyyy-MM-dd");
                var dataTable = vt.DataTableOlustur("select * from psip_V1.TblEventDetail where SeoUrl= '" + seoUrl + "'", CommandType.Text);
                if (dataTable.Rows.Count > 0)
                    continue;

                var foodDayMenu = new Root
                {
                    MainFood = food.ToList(),
                    DietFood = foodList.DietFood.Where(df => df.Date == food.Key && !string.IsNullOrEmpty(df.DietMainDish)).ToList(),
                    Salad = foodList.Salad.Where(df => df.Date == food.Key).ToList(),
                };
                List<psip_DBParameter> param = new List<psip_DBParameter>
                {
                    new psip_DBParameter("@CreatedById", SqlDbType.Int, 1),
                    new psip_DBParameter("@AssignedById", SqlDbType.Int, 1),
                    new psip_DBParameter("@Visibility", SqlDbType.TinyInt, 1),
                    new psip_DBParameter("@LanguageId", SqlDbType.SmallInt, 31),
                    new psip_DBParameter("@TagIds", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@ContentIds", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@FileIds", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@MemberIds", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@CategoryIds", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@EventTypeId", SqlDbType.Int, 11),
                    new psip_DBParameter("@Title", SqlDbType.Text, city.Key + " Yemek Listesi"),
                    new psip_DBParameter("@ShortDescription", SqlDbType.Text, DBNull.Value),
                    new psip_DBParameter("@Description", SqlDbType.Text, GetFoodHtml(foodDayMenu)),
                    new psip_DBParameter("@IsLatest", SqlDbType.Bit, 0),
                    new psip_DBParameter("@StartDate", SqlDbType.DateTime, foodDayStr),
                    new psip_DBParameter("@EndDate", SqlDbType.DateTime, foodDayStr),
                    new psip_DBParameter("@Phone", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Email", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Web", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Contact", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@CountryId", SqlDbType.Int, DBNull.Value),
                    new psip_DBParameter("@CityId", SqlDbType.Int, DBNull.Value),
                    new psip_DBParameter("@Address", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Latitude", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Longitude", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@Venue", SqlDbType.NVarChar, " Kampüs"),
                    new psip_DBParameter("@RepeatState", SqlDbType.NVarChar, DBNull.Value),
                    new psip_DBParameter("@SeoUrl", SqlDbType.NVarChar, seoUrl)
                };

                string startTime = "12:00";
                param.Add(new psip_DBParameter("@StartTime", SqlDbType.Time, TimeSpan.Parse(startTime)));

                string endTime = "14:00";
                param.Add(new psip_DBParameter("@EndTime", SqlDbType.Time, TimeSpan.Parse(endTime)));

                vt.ScalarSorguCalistir(spName, CommandType.StoredProcedure, param.ToArray()).ToStringByDefaultValue();
            }
        }

        vt.Kapat();
    }
}



