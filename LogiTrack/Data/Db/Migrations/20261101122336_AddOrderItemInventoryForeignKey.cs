using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiTrack.Data.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemInventoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Items" ("ItemId", "Name", "Quantity", "Location")
                SELECT orderItem."ItemId", MIN(orderItem."Name"), 0, MIN(orderItem."Location")
                FROM "OrderItem" AS orderItem
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Items" AS item
                    WHERE item."ItemId" = orderItem."ItemId")
                GROUP BY orderItem."ItemId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ItemId",
                table: "OrderItem",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Items_ItemId",
                table: "OrderItem",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Items_ItemId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_ItemId",
                table: "OrderItem");
        }
    }
}
