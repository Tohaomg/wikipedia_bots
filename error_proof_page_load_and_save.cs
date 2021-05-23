using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using DotNetWikiBot;

namespace ErrorProofPageLoadAndSave
{
 //following functions load and save pages with error handling, so that if an error occurs it does not crash the programm,
 //instead of this, the error is written to the log and the programm continues to run
 public static class ErrorProofPageFunctions
 {
  private static Mutex error_log_writing_mutex = new Mutex(); //mutext needed to make sure that in any point of time only one error message is being written to the file
  private static Mutex save_page_mutex = new Mutex(); //mutext needed to make sure that there are no simultaneous saves

  //write an error message to a log
  private static void LogError(Exception ex, string title, string stage, int id)
  {
	error_log_writing_mutex.WaitOne();
	Console.WriteLine("Unknown or repeated error");
	System.IO.StreamWriter errors_log = new System.IO.StreamWriter("errors_log.txt", true);
	errors_log.WriteLine(title + "\t" + stage + "\t" + id + "\t" + ex.GetType().Name + "\t" + ex.Message);
	errors_log.Close();
	error_log_writing_mutex.ReleaseMutex();
  }

  //load the page
  public static bool ErrorProofPageLoad(ref Page p, string site_url)
  {
	int errors_count = 0;
	load:
	try {p.Load();} //try to load the page
	catch(WebException ex) //if it is WebException, then it will retry after waiting for some time (but not more than five times in a row)
	{
		errors_count++;
		Console.WriteLine("The remote server returned an error. Try again in 20 seconds.");
		Thread.Sleep(20000);
		if(errors_count>=5) {LogError(ex, p.title, "load", 1); return true;}
		else {goto load;}
	}
	catch(NullReferenceException) //if it is NullReferenceException, then it will try to load the page text directly via API
	{
		Console.WriteLine("Object reference not set to an instance of an object. Trying to reinitialize the object.");
		try
		{
			using (WebClient client = new WebClient())
			{client.Encoding = Encoding.UTF8; p.text = client.DownloadString("htt" + "ps://" + site_url + "/w/index.php?title=" + p.title.Replace(" ", "_") + "&action=raw");}
		}
		catch(Exception ex) //if the reloading was not successful, log the error and skip the page
		{
			Console.WriteLine("Failed to load page directly via API.");
			LogError(ex, p.title, "load", 2);
			return true;
		}
	}
	catch(Exception ex) //if the exception is none of the above, log it as an unknown error and skip the page
	{
		Console.WriteLine("Unknown error in load. Log and skip.");
		LogError(ex, p.title, "load", 3);
		return true;
	}
	//if an article is marked as being currently edited, then it should be skipped
	if(p.text.Contains("{{in use") || p.text.Contains("{{In use") || p.text.Contains("{{edit") || p.text.Contains("{{Edit")) {return true;}
	return false;
  }

  //saves changes to a page
  public static bool ErrorProofPageSave(ref Page p, ref Site wiki, string save_comment, string password)
  {
	save_page_mutex.WaitOne(); //there can be only one save at any point in time
	try{p.Save(save_comment, true);} //try to savethe page
	catch(EditConflictException) //if it is EditConflictException, then page should be reloaded and processed again
	{
		Console.WriteLine("Edit conflict occured. Processing this page again.");
		Thread.Sleep(3000);
		return true;
	}
	catch(WikiBotException ex)
	{
		if(ex.Message=="Invalid CSRF token.") //if toke is invalid, try to login again
		{
			Console.WriteLine("Invalid CSRF token. Trying to relogin.");
			wiki = new Site("https://uk.wikipedia.org", "TohaomgBot", password);
		}
		else {Console.WriteLine(ex.Message);}
		Thread.Sleep(3000);
		try {p.Save(save_comment, true);} //try to save again, after repeating the login
		catch(Exception ex2) //if the second attempt was not successful as well, log the error
			Console.WriteLine("Second attempt of saving failed.");
			LogError(ex2, p.title, "save", 4);
			save_page_mutex.ReleaseMutex(); return false;
		}
	}
	catch(WebException) //WebException means some problems with internet connection, in this case it will try to save again
	{
		Console.WriteLine("The remote server returned an error. Trying to save again.");
		try {p.Save(save_comment, true);}
		catch(Exception ex2) //if the second attempt was not successful as well, log the error and skip the page
		{
			Console.WriteLine("Second attempt of saving failed.");
			LogError(ex2, p.title, "save", 4);
			save_page_mutex.ReleaseMutex(); return false;
		}
	}
	catch(Exception ex) //if the exception is none of the above, log it as an unknown error
	{
		Console.WriteLine("Unknown error in save. Log and skip.");
		LogError(ex, p.title, "save", 5);
		save_page_mutex.ReleaseMutex(); return false;
	}
	save_page_mutex.ReleaseMutex();
	return false;
  }
 }
}