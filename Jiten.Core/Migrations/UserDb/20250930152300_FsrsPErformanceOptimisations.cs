using Microsoft.EntityFrameworkCore.Migrations;

namespace Jiten.Core.Migrations.UserDb;

    public partial class FsrsPerformanceOptimizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BRIN indexes for time-based queries
            migrationBuilder.Sql(@"
                CREATE INDEX ix_fsrscards_due_brin ON user.""FsrsCards"" 
                USING brin (""Due"") 
                WITH (
                    pages_per_range = 128,
                    autosummarize = on
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX ix_fsrsreviewlogs_datetime_brin ON user.""FsrsReviewLogs"" 
                USING brin (""ReviewDateTime"") 
                WITH (
                    pages_per_range = 128,
                    autosummarize = on
                );
            ");

            // Partial index for active cards due soon
            migrationBuilder.Sql(@"
                CREATE INDEX ix_fsrscards_due_active ON user.""FsrsCards"" (""Due"", ""State"")
                WHERE ""State"" IN (1, 2, 3) AND ""Due"" < NOW() + INTERVAL '7 days';
            ");

            // Partial index for recent review logs
            migrationBuilder.Sql(@"
                CREATE INDEX ix_fsrsreviewlogs_recent ON user.""FsrsReviewLogs"" (""CardId"", ""ReviewDateTime"")
                WHERE ""ReviewDateTime"" > NOW() - INTERVAL '90 days';
            ");

            // Compression settings
            migrationBuilder.Sql(@"
                ALTER TABLE user.""FsrsCards"" SET (toast_compression = lz4);
                ALTER TABLE user.""FsrsReviewLogs"" SET (toast_compression = lz4);
            ");

            // Fill factor for update performance
            migrationBuilder.Sql(@"
                ALTER TABLE user.""FsrsCards"" SET (fillfactor = 90);
            ");

            // Autovacuum settings
            migrationBuilder.Sql(@"
                ALTER TABLE user.""FsrsCards"" SET (
                    autovacuum_vacuum_scale_factor = 0.05,
                    autovacuum_analyze_scale_factor = 0.02
                );
                
                ALTER TABLE user.""FsrsReviewLogs"" SET (
                    autovacuum_vacuum_scale_factor = 0.1,
                    autovacuum_analyze_scale_factor = 0.05
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all the indexes and revert settings
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS user.ix_fsrscards_due_brin;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS user.ix_fsrsreviewlogs_datetime_brin;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS user.ix_fsrscards_due_active;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS user.ix_fsrsreviewlogs_recent;");
            
            // Revert table settings to defaults
            migrationBuilder.Sql(@"ALTER TABLE user.""FsrsCards"" RESET (toast_compression, fillfactor, autovacuum_vacuum_scale_factor, autovacuum_analyze_scale_factor);");
            migrationBuilder.Sql(@"ALTER TABLE user.""FsrsReviewLogs"" RESET (toast_compression, autovacuum_vacuum_scale_factor, autovacuum_analyze_scale_factor);");
        }
    }
