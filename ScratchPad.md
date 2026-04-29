
In Sending and the last line item under Create has a Delete button but it is disabled, clicking Delete sets is_active = false for that line item.

Under Receiving if I add a line item (it being the last line item) the Delete button is not visible.

ATATT3xFfGF0NSa28-3k6wQrE1frrJfi_7EpQqTbsuBctNeG7Q6LqugQlUnX6aTw_XcjftJKCQs9thm_GuTDvyaUxL2QXB3dT9m0I0Gf1zy269DxBhzASKTUEuVfCXFR-buGXh8DIgzqSBPSrU9WSHOWkBJ2IrPhE9I7s71ymi67iErRzE3Du2Y=E256729E

JK - ATATT3xFfGF0HtUc2GMOsGM4pJ0d2KPi7EgfIlDqhtwkcQNL8n82PchOhno2O-ti9bE8nkBzhwf5aqHW_4ejZcXB0VXGOLwMldtG7hUuWrfZUyvn7eM7VapXlKLW05AQY3EpqZWPbeBjfQmR-LoFtbpj5-_2teh85gKf01YRL9TGe8MYXZJuUpc=8991D4E7

Please go through the solution and familiarise yourself with the systems. This is a solution  for a scrap metal company we have a "Customers" system who we buy scrap from in the "Receiving" system. We have a "Buyers" system who we sell scrap to in the "Sending" system. When I refer to [both] I am referring to both Receiving and Sending.

The following gives insights to the system: 

We NEVER hard delete a record we set is_active value = false, ALL tables have this column and we only retrieve tables where is_active = true

New Tickets are created in the receiving_tickets and sending_tickets tables.




Hello 

I own Elementech Digital Solutions, a new start up. I'm developing an application for metal recycling industry, with Barloworld Automotive, we collaborated with a company called TransUnions who produce the Mead & McGrouther (Mead & McGrouther (M&M) is a South African automotive industry standard guide, now owned by TransUnion, that provides comprehensive retail and trade valuation data for vehicles. Known as the "bible" of car prices since 1960, it serves as the authoritative source for identifying, valuing, and tracking used car prices, often referred to as the "MMCODE" or "book value" in insurance and banking. ) for my system having a Isri specifications feed could be a big thing for the recycling industry as every company now has a different specifications list. I'm interested in an Isri API for world wide usage, is this something we could maybe consider as a joint venture? My contact number is +27 63 6377091. I hope to hear from you a subscription service at even $20 a month could be very lucrative for you and it would help grow my product offering. Everybody wins.

Okay now we are creating new tables and later we'll change products. All ids are ints. And all column are non nullable unless stated.

ALL tables have these 4 columns:
- created_by_operator_id (FK -> PK operators.operator_id)
- is_active (bool, default: true) - we don't delete records from the DB, we soft delete them by setting is_active = false
- created_time (datetimeoffset, default: now())
- updated_time (datetimeoffset, default: now())

Please create 2 new tables:

1) product_specification_flags columns:

- product_specification_flag_id (int, PK, auto increment)
- product_specification_type_flag (char)
- product_specification_description
- created_by_operator_id
- is_active
- created_time
- updated_time

with 3 records: 
'N' | Nonferrous
'F' | Ferrous
'C' | Custom

2) product_groups:

- product_group_id (int, PK, auto increment)
- product_group_name
- product_group_description
- product_specification_flag_id (FK -> PK: product_specification_flags.product_specification_flag_id)
- created_by_operator_id
- is_active
- created_time
- updated_time

And populate with these records (Make up a suitable description for each group name for the description column):

product_specification_flag_id = product_specification_flags.product_specification_flag_id WHERE product_specification_flag_description = Nonferrous

Aluminum
Lead
Magnesium
Mixed Metals
Nickel/Stainless/Hi-Temp
Other
Red Metals
Zinc

product_specification_flag_id = product_specification_flags.product_specification_flag_id WHERE product_specification_flag_description = Ferrous

Ferrous
Ferrous - EF Cast & Foundry
Ferrous - Special (Boring)
Ferrous - Special (Cast Iron)
Ferrous - Special (Railroad)
Ferrous - Special (Scrap Tire)

Rename the prices table to legacy_prices.

Okay next step:

I made a mistake in nameing prices -> legacy_prices, please change legacy_prices back to prices. then please rename products to legacy_products

Please create a new table "products" with the following columns:

product_id
hts_code
isri_product_code
isri_product_name
isri_product_description
isri_product_url
isri_product (bool default = false)
product_group_id (FK -> PK: product.groups.product_group_id)
product_specification_flag_id (FK -> PK: product_specification_flags.product_specification_flag_id)
starred_product (bool, default false)
starred_product_alias
must_declare (bool default false)
created_by_operator_id
is_active
created_time
updated_time

Now to populate these tables I need you to get the data from the ISRI site you might to store it in a temp products table till you have everything:

Please review my request and first please confirm if you are able to harvest the data in this manner.

On each of the group pages below is a list of codes and items (products), use this for: isri_product_code, isri_product_name

https://www.isrispecs.org/orpheus_resource_categories/nonferrous-scrap/

Aluminum - https://www.isrispecs.org/orpheus_resource_categories/aluminum/
Lead - https://www.isrispecs.org/orpheus_resource_categories/lead/
Magnesium - https://www.isrispecs.org/orpheus_resource_categories/magnesium/
Mixed Metals - https://www.isrispecs.org/orpheus_resource_categories/mixed-metals/
Nickel/Stainless/Hi-Temp - https://www.isrispecs.org/orpheus_resource_categories/nickel-stainless-hi-temp/
Other - https://www.isrispecs.org/orpheus_resource_categories/other/
Red Metals - https://www.isrispecs.org/orpheus_resource_categories/copper/
Zinc - https://www.isrispecs.org/orpheus_resource_categories/zinc/

https://www.isrispecs.org/orpheus_resource_categories/nonferrous-scrap/

Ferrous - https://www.isrispecs.org/orpheus_resource_categories/ferrous/
Ferrous - EF Cast & Foundry - https://www.isrispecs.org/orpheus_resource_categories/ferrous-ef-cast-foundry/
Ferrous - Special (Boring) - https://www.isrispecs.org/orpheus_resource_categories/ferrous-special-boring/
Ferrous - Special (Cast Iron) - https://www.isrispecs.org/orpheus_resource_categories/ferrous-special-cast-iron/
Ferrous - Special (Railroad) - https://www.isrispecs.org/orpheus_resource_categories/ferrous-special-railroad/
Ferrous - Special (Scrap Tire) - https://www.isrispecs.org/orpheus_resource_categories/ferrous-special-scrap-tire/

If an item is selected from the group product listing, this page (as an example) loads: https://www.isrispecs.org/orpheus_resource/clean-aluminum-lithographic-sheets/ this url is the "isri_product_url" value this page contains the isri_product_code again (for confirmation) (it's called ITEM CODE on the page), hts_code, isri_product_description.

product_group_id you can defer from the information you have
isri_product = true
product_specification_flag_id this will be 1 or 2 depending if Nonferrous or Ferrous

starred_product_alias -> This is tricky, it must be done for ALL products in legacy_products so keep track of them and let me know if you cant matchup any of the products in legacy_products to products, list them afterwards. Based on every product's product_code and product_name in legacy_products table find the most relevant isri_product_description in products and populate the starred_product_alias column for that record with legacy_products.product_name.

For these records that have been matched, starred_product = true.
must_declare is true for all Red Metal (Copper) products.


------------

For Products: Great but I dont think you generated the images of recyclable goods that each contain the product specification (i.e. a picture of aluminium cans for the specification of product we use that specific aluminum product/specification) for the roughly 300 products we have please generate and store them in MinIO because the popup has no image. The drop down for results per page is empty, it says 2 pages (so 40 products) but says ( 50 Total )

For Price Lists: if I select and Entity Type of Customer or buyer or enter a few characters that appear in the price list name it does not filter accordingly, so I can't Test the reset button. If I cahnge the Entity Type of Price List Search Name the results must adjust accordingling. For Create/Edit Pricelists, Create should be default to Customer for Entity type and must be populated with the selected value record from Price List Results. Add Entity Type column to the Price Lists table after Description.

Change Create Price List to Create/Edit Price Lists, remove the Active tickbox and add Entity Type before Name in Create/Edit, the Datagrid of Price LIst results: add the product_price_list_description Description column. I’ve removed Active column. Change the Edit button to look like other datagrid buttons and change it to Delete, Which uses a Confirmation popup. Then soft deletes (sets is_active = false) for that Price List.

Mark all data in stock_levels and stock_movements tables is_active = false. Create opening balance values for stock_levels for starred products. Then generate data for stock_movements (of starred_products = true,only) about 4 to 8 movements (buy or sell) per starred product per day, And adjust stock_level values for starred products accordingly.

You added Product Group to Stock Levels and Stock Movement Systems Systems. But now the grid is empty on load Product Group should be All, changing Product Group does nothing there is no graph and nor results, even if I select ALL, all systems including Buying, Receiving, Stock Levels and Stock Movements, only work with products where starred_product = true;.

------------

Okay working now but the stock_movements data is nowhere near what I asked so please mark all records for stock_levels and stock_movements = false. Then for the 50 starred_products = true, create an initial stock level and then create approximately 3 to 7 purchases (100 to 400kg’s) and 1 or 2 sales per day (1500 to 300 kgs) Sales can only be for amounts we have (stock openeing and then purchases till that point.) so 57 to 7 purchases mixed up with 1 to 2 sales (of available stock) for every starred product for every day for the last 2 months.

Can't really speak ... shutting down  the steering wheel ... countermeasure to panic attack... I'm on headphones focused but foggy blocking everything out.. Mom ... tooo complicated, ...... please provide a message truama to much not responding not even in an emergency if possible.... she hates me so much says they want to help then go mad when I say other parents support their kids they can't name 2 people. Here messages but focus on

That's why I didn't come today, why I had to beg for an Uber ... 3 times, you have so much hate and disdain I can't believe Dad attacks me but the shit you say 

------->

Can't really speak ... shutting down  the steering wheel ... countermeasure to panic attack... I'm on headphones focused but foggy blocking everything out.. Mom ... tooo complicated, ...... please provide a message truama to much not responding not even in an emergency if possible.... she hates me so much says they want to help then go mad when I say other parents support their kids they can't name 2 people. Here messages but focus on

That's why I didn't come today, why I had to beg for an Uber ... 3 times, you have so much hate and disdain I can't believe Dad attacks me but the shit you say .... Soul crushing I was a bad provider and thats why Heidi left me???? I bought my nephling and neicling plastic Y catapult thing that Bart has and Dennis the menace it shoots things with an elastic bridge and it come with a foam ball ...... so confused. She shit me out. She hates me so much and shes poisoning my WHOLE family against me..... She's trying to kill me...... I lost my children and wife and have nothing my sister in law cried for 3 days and was devestated!!!! I've grieved like that for 4 years and 10 months and 20 days... I can't cut her off because then I'm shunning and a bad person Aunty Wendy wants me out.

Something to just stop her now and she even knows about my LBD so cruel Heidi said nothing not even the reason my Mom says everything every single thing I do to feeding kids from broken homes how the fuck, I'm not their problem 


Do you understand how products -> product lists -> pricing works? We finished the systems, Products and Price Lists. Please build the Prices System for me, the top section will be a panel/section for Price List Selection, at the top is Entity Type, then there are 4 drop down lists below, they contain a list of Price Lists (for Customer/Buyer depending on Entity Type) the operator can select up to 4 price lists to add to the results say we have 5 price lists for Customer Price List A, Customer Price List B, Customer Price List C, Customer Price List D, Customer Price List E -> if you select Customer Price List B for the first dropdown, the Product Prices Datagrid (below) has a column added with the prices (the prices are editable in the datagrid and update the value in the DB, when changed), the remaining 3 drop downs only contain non selected price lists so: Customer Price List A, Customer Price List C, Customer Price List D, Customer Price List E, below that a panel or section for  Product Filters like the Products system. And below that the Product Prices (for all starred_products = true) datagrid columns: HTS Code, ISRRI CodeAlias (or if Alias is null the ISRI Name Groupand then prices column1 will become visible when selected from the first price list dropdown, then the secon column of prices will appear for the second selected price list, and so forth for the 3rd and 4th optional priceslists to edit.)


Please fix the 2 x Price Lists systems (Price List Stock Levels && Price List Stock Movements)... they are the same as Stock Levels and Stock Movements but instead of Stock Levels for a range of products. Price List Stock Levels is for 1 product and lines/records for each price list level. Stock Movements again isn't per product it's per Price List, so 1 product with lines for each price list.

Both are like Stock Levels and Stock Movements but underneath product filter/search criteria there is another panel with a datagrid (first column tick boxes) of all price lists for the selected product.

Currently for these 2 x Price Lists systems at the moment there are no panels or sections. There is no background panels, just the brushed metal application background. Please use the base system Stock Levels -> Price List Stock Levels and Stock Movement -> Price List Stock Movements and add the price list datagrid section under Search/Filter Criteria.

Under Receiving and Sending the Product Group dropdown is empty for Filter/Search.

We are going to do a stock clear and reseed with fresh data.

Please mark all records in the following as is_active = false: receiving_ticket_lines, sending_ticket_lines, stock_levels, stock_movements.

Once they've been soft deleted, in those 4 tables please create seed data for the last 3 months and 3 months in the future. The ones from today and in to the future, please mark is_active = false (as time passes I'll write a script to make them is_active = true;)

Here are the parameters: Create purchases and sales in all 4 tables. Approximately 10 to 14 line items per ticket. Approximately 60 to 80 purchases a day (Mon -> Fri). Approximately 4 to 10 sales a day (Mon -> Fri).

Tickets have between 4 and 18 line items.

Select random weights. Purchases (platform/weighbridge tickets): Between 10kg and 800kg for Platform tickets and between 200kg and 4000kg's for weighbridge. For Sales: between 200kg and 4000kg's for weighbridge

So for that period you're going to create purchase and sales tickets (INSERT into receiving_tickets & sending_tickets), then create the line items as instructed for each ticket (INSERT into receiving_ticket_lines, sending_ticket_lines, adjust/map stock_levels and stock_movements tables)

This will give us base data to work with for 3 months past and 3 months in the future.



-------------------

Information regarding Ticket Numbers. - Ticket Numbers are auto generated with the following prefix (please fix the records you seeded): RPL- Receiving Platform, RWB- Receiving Weighbridge, SPL- Sending Platform, SWB- Sending Weighbridge. The format is Prefix followed by 8 padded digits i.e.: RPL-00000008 then RPL-00000009 then RPL-00000010, etc.