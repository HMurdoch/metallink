I checked the DB the product_price_list_product_price_id to -> stock_movements, stock_levels, receiving_ticket_lines, sending_ticket_lines and changing the relationships.

You didn't add the other columns to the DB.

Okay here are the rules for the product_price_list_product_price to the solution:

This is a big change for stock_levels and stock_movements. after you add the columns:

stock_levels, now have product_id and product_price_list_product_price_id -> for product XX there could be 3 records (is_active=true) for 3 different price lists. For stock levelsthe results are the sum of each of the price list values for that product.

Add a system (use Stock Levels as the reference system) "Price List Stock Distribution" like the Stock Levels system to include Product Filters section (left column), then a panel on the right with Entity Type (Customer/Buyer) and then a paged list (10 records per increment) of price lists for that entity first column is tick boxes, these are listed in a datagrid above the graph with paged (pages in increments of 10) we'll be able to break up in to stock level per price list.

Important when a new price list is created or a product is added or made starred_product = true a record must be created in stock_levels with product_id, product_price_list_products_price_id (new price list or price list of added product), weight_kg is 0.0. When a pricelist is deleted or a product starred_product = false is_active for that product_id AND product_price_list_product_price_id is set to false.

Then the same will apply for Stock Movements, for each product the movement is for every record in stock_movements. (stock_movements is added to when a line item is added for Receiving and Sending) if a line item is added/removed the item is stracked in (stock_movements) AND (receiving_ticket_lines OR sending_ticket_lines) add a new System underneath Stock Movements call Price List Stock Movements, which will then also contain a section next to Stock Movement Filters called Pricelists also with Entity Type then a datagrid of Price Lists (both this and Price Levels Stock Distribution datagrids are paged at 10 per increment. The graph will now have the dimension of pricelists (Z) so if 4 pricelists are ticked the graph has 4 lines with stock level for that price list over time.

For Stock Movements and Price List Stock Movements the graph must a "Link" (with chain link icon) which is default chained (= true) if so, scrolling the mouse wheel on the graph zooms the time frame in and out focused on the time the mouse is hovering over.

Please fix the DB and wire this all in, please check if you are unsure before changing anything.

When stock changes (e.g., a receiving ticket adds 100kg of Product X), instead of putting all 100kg into one stock level record, the system distributes the change proportionally across all active price list stock levels for that product. -> NO this is incorrect, it goes in to one stock level record for each unique combination of product_id and product_price_list_product_price_id. The full amount was that was bought/sold is added to (receiving_ticket_line OR sending_ticket_lines) AND stock_movements the full weight for that product_id and product_price_list_product_price_id. Does that make sense