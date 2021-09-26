### Build
```sh
dotnet publish -c Release -o output_directory
```
### Add cron job
- `crontab -e`
```sh
# e.g. every 5 minutes
*/5 * * * * cd path_to_project && ./path_to_executable feeds.json >> out.log 2>&1
```
