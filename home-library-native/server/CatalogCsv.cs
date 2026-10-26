using System.Text;
namespace HomeLibrary;
public sealed record CsvText(string Text);
public static class CatalogCsv {
 public static object Parse(string? text){
  if(text==null||text.Length>10*1024*1024)throw new LibraryError("CSV must be under 10 MB.");text=text.TrimStart('\uFEFF');var first=text.Split('\n')[0];var delimiter=new[]{',',';','\t'}.OrderByDescending(d=>first.Count(c=>c==d)).First();
  var records=new List<List<string>>();var row=new List<string>();var cell=new StringBuilder();bool quote=false,closed=false;
  void EndRow(){row.Add(cell.ToString());if(row.Any(v=>v!=""))records.Add(row);row=new();cell.Clear();closed=false;if(records.Count>1001)throw new LibraryError("Split CSV exports into batches of up to 1000 books.");}
  for(var i=0;i<text.Length;i++){var c=text[i];if(quote){if(c=='"'){if(i+1<text.Length&&text[i+1]=='"'){cell.Append('"');i++;}else{quote=false;closed=true;}}else cell.Append(c);continue;}
   if(c=='"'){if(cell.Length>0||closed)throw new LibraryError("Unexpected quote in CSV.");quote=true;}else if(c==delimiter){row.Add(cell.ToString());cell.Clear();closed=false;}else if(c is '\n' or '\r'){if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;EndRow();}else{if(closed&&!char.IsWhiteSpace(c))throw new LibraryError("Unexpected text after CSV quote.");if(!closed)cell.Append(c);}}
  if(quote)throw new LibraryError("Unclosed quoted CSV field.");EndRow();if(records.Count<2)throw new LibraryError("CSV needs headers and at least one book.");var headers=records[0];records.RemoveAt(0);if(headers.Count>100||headers.Any(string.IsNullOrWhiteSpace)||headers.Select(h=>h.Trim().ToLowerInvariant()).Distinct().Count()!=headers.Count||records.Any(r=>r.Count!=headers.Count))throw new LibraryError("Use unique headers and matching CSV columns.");return new{headers,records};
 }
}
