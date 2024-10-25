using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using Notion.Client;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NotionDB2iCal.Controllers
{
    public class iCalController : Controller
    {
        //only track 180 days
        readonly TimeSpan DaysBefore = TimeSpan.FromDays(180);

        [HttpGet]
        [Route("/api/iCal")]
        public async Task<IActionResult> GetiCalAsync([FromQuery] string db)
        {
            var client = NotionClientFactory.Create(new ClientOptions
            {
                AuthToken = Environment.GetEnvironmentVariable("token")
            });

            var databaseId = db;
            var dateFilter = new DateFilter("日期", onOrAfter: DateTime.Now - DaysBefore);
            var queryParams = new DatabasesQueryParameters { Filter = dateFilter };
            var pages = await client.Databases.QueryAsync(databaseId, queryParams);

            var calendar = new Calendar();
            foreach (var page in pages.Results)
            {
                var name = page.Properties["名稱"] as Notion.Client.TitlePropertyValue;
                var date = page.Properties["日期"] as Notion.Client.DatePropertyValue;
                var desc = page.Properties["備註"] as Notion.Client.RichTextPropertyValue;

                var calendarEvent = new CalendarEvent
                {
                    Summary = name.Title[0].PlainText, // Should always be present
                };

                if (desc.RichText.Any())
                    calendarEvent.Description = desc.RichText[0].PlainText;
                if (date.Date.Start.HasValue)
                    calendarEvent.Start = new CalDateTime(date.Date.Start.Value.ToUniversalTime());
                if (date.Date.End.HasValue)
                    calendarEvent.End = new CalDateTime(date.Date.End.Value.ToUniversalTime());

                calendar.Events.Add(calendarEvent);
            }

            calendar.AddTimeZone(new VTimeZone("Asia/Taipei")); // TZ should be added

            var serializer = new CalendarSerializer();
            var serializedCalendar = serializer.SerializeToString(calendar);

           
            return File(Encoding.UTF8.GetBytes(serializedCalendar), "application/octet-stream", "cal.ics");

        }
    }
}
