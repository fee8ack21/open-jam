using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailConfigToEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 純改名：保留既有資料，逐一改 table / column / index / constraint 名稱。
            migrationBuilder.RenameTable(
                name: "email_config_translations",
                newName: "email_template_translations");

            migrationBuilder.RenameTable(
                name: "email_configs",
                newName: "email_templates");

            migrationBuilder.RenameColumn(
                name: "template_key",
                table: "email_templates",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "email_config_id",
                table: "email_template_translations",
                newName: "email_template_id");

            migrationBuilder.RenameIndex(
                name: "ix_email_configs_template_key",
                table: "email_templates",
                newName: "ix_email_templates_key");

            migrationBuilder.RenameIndex(
                name: "ix_email_config_translations_email_config_id_locale",
                table: "email_template_translations",
                newName: "ix_email_template_translations_email_template_id_locale");

            migrationBuilder.Sql(
                "ALTER TABLE email_templates RENAME CONSTRAINT pk_email_configs TO pk_email_templates;");
            migrationBuilder.Sql(
                "ALTER TABLE email_template_translations RENAME CONSTRAINT pk_email_config_translations TO pk_email_template_translations;");
            migrationBuilder.Sql(
                "ALTER TABLE email_template_translations RENAME CONSTRAINT fk_email_config_translations_email_configs_email_config_id TO \"fk_email_template_translations_email_templates_email_template_\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE email_template_translations RENAME CONSTRAINT \"fk_email_template_translations_email_templates_email_template_\" TO fk_email_config_translations_email_configs_email_config_id;");
            migrationBuilder.Sql(
                "ALTER TABLE email_template_translations RENAME CONSTRAINT pk_email_template_translations TO pk_email_config_translations;");
            migrationBuilder.Sql(
                "ALTER TABLE email_templates RENAME CONSTRAINT pk_email_templates TO pk_email_configs;");

            migrationBuilder.RenameIndex(
                name: "ix_email_template_translations_email_template_id_locale",
                table: "email_template_translations",
                newName: "ix_email_config_translations_email_config_id_locale");

            migrationBuilder.RenameIndex(
                name: "ix_email_templates_key",
                table: "email_templates",
                newName: "ix_email_configs_template_key");

            migrationBuilder.RenameColumn(
                name: "email_template_id",
                table: "email_template_translations",
                newName: "email_config_id");

            migrationBuilder.RenameColumn(
                name: "key",
                table: "email_templates",
                newName: "template_key");

            migrationBuilder.RenameTable(
                name: "email_templates",
                newName: "email_configs");

            migrationBuilder.RenameTable(
                name: "email_template_translations",
                newName: "email_config_translations");
        }
    }
}
