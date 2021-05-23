using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Globalization;
using DotNetWikiBot;
using ErrorProofPageLoadAndSave;

class DuplicateReferencesRemover:Bot
{
 public static PageList pages; //variable for the list of pages to process
 public static Site wiki; //project (site) where the bot will be running
 public static Mutex start_thread_mutex = new Mutex(); //mutex to make threads launch consecutively and not simultaneously
 public static CultureInfo culture = new CultureInfo("uk-UA");
 public static string page_list_count_str, username, password, edit_comment, site_url;
 public static int pages_count, thread_counter = 0, threads_count = Environment.ProcessorCount;
 public static DateTime start_moment;
 
 //what each of the threads should do
 public static void OneThreadOfPageProcessing()
 {
  //get the index of current thread
  start_thread_mutex.WaitOne();
  int thread_index = thread_counter++;
  start_thread_mutex.ReleaseMutex();

  Console.WriteLine("STARTED THREAD " + thread_index);
  MatchCollection mc;
  Page p;

  DateTime current_moment; //variable to store current moment (needed to calculate remaining time)
  int timespan, est_time_left, days, hours, minutes;
  string estimated_time_left_str;

  bool should_skip_this_page = false;
  bool should_goto_load = false;

  //iterate over pages in the list so that first thread processes all pages with indexes in list divisible by number of threads
  //second thread processes all pages with index giving a remainder 1 when divided on number of threads, third threads with remainder 2 and so on
  for(int i=thread_index ; i<pages_count ; i+=threads_count)
  {
	p = pages[i];

	load:

	//calculate estimated time left
	if(i!=0)
	{
		current_moment = DateTime.Now; //get timestamp of current moment
		timespan = (int)(current_moment - start_moment).TotalSeconds; //get ammount of time that elapsed since the programm was launched
		est_time_left = (int)Math.Ceiling( (double)(timespan*(((double)(pages_count)-(double)(i))/(double)(i))/60) ); //calculate estimated time remaining
		minutes = est_time_left%60; //get amount of minutes
		est_time_left = (est_time_left-minutes)/60;
		hours = est_time_left%24; //get amount of hours
		est_time_left = (est_time_left-hours)/24;
		days = est_time_left; //get amount of days
		estimated_time_left_str = (days!=0?days+" d ":"") + (hours!=0?hours+" h ":"") + minutes+" m"; //present estimated time remaining in human readable text form
	}
	else {estimated_time_left_str = "?";} //there is nothing to show when no pages were processed yet
	Console.Write("thread " + thread_index + ":   " + i.ToString("#,#", culture) + " / " + page_list_count_str + "  (" + Math.Floor( (double)((i*100)/pages_count) ) + "%, " + estimated_time_left_str + ")   " );

	//load the page (function ErrorProofPageLoad returns 'true' if this page should be skipped)
	should_skip_this_page = ErrorProofPageFunctions.ErrorProofPageLoad(ref p, site_url);
	if(should_skip_this_page) {continue;}

	if( !(p.text.Contains("fbclid") || p.text.Contains("igshid")) ) {continue;} //there is no point in processing the page if there are no 'fbclid's or 'igshid's

	//look for 'fbclid' and 'igshid' tokens using a regexp
	mc = Regex.Matches(p.text, @"(\?|\&)((?:fbclid|igshid)=[a-zA-Z0-9_\-]+)(.)", RegexOptions.Singleline);
	if(mc.Count==0) {continue;} //if none were found, there is no point in processing the page further
	foreach(Match m in mc) //iterate over each found value
	{
		if(m.Groups[1].Value=="?" && m.Groups[3].Value=="&") //if 'query' part of URL contains something else, apart from 'fbclid' or 'igshid' token, and the token is first in this 'query' part
		{p.text = p.text.Replace(m.Groups[0].Value, "?");} 
		else //otherwise, remove 'fbclid' or 'igshid' token together with '?' or '&' character
		{p.text = p.text.Replace(m.Groups[1].Value + m.Groups[2].Value, "");}
	}

	//save the page (function ErrorProofPageLoad returns 'true' if processing of this page should start again, from the beginning)
	should_goto_load = ErrorProofPageFunctions.ErrorProofPageSave(ref p, ref wiki, edit_comment, password);
	if(should_goto_load) {goto load;}
  }
 }

 //here is a point of entrance to the programm: its execution starts from function 'main', which receives data passed to it from 'bat' file in array 'args'
 public static void Main(string[] args)
 {
  ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
  StreamReader pass_in = new StreamReader("login_data.txt"); //read username and password from 'login_data.txt' text file
  username = pass_in.ReadLine();
  password = pass_in.ReadLine();
  pass_in.Close();
  site_url = args[0]; //first parameter received from 'bat' file is an URL of project where the bot will be working
  edit_comment = args[3]; //fourth parameter received from 'bat' file is an edit comment used when saving changes to any of the pages
  wiki = new Site("https://" + site_url, username, password); //login to the project

  //depending on what is there in the second and third parameters received from 'bat' file, write which pages will be processed
  if(args[1]!="" && args[1].Contains(".txt")) {Console.WriteLine("Processing pages listed in " + args[1]);}
  else if(args[1]!="" && args[2]!="") {Console.WriteLine("Processing from [" + args[1] + "] to [" + args[2] + "]");}
  else if(args[1]!="") {Console.WriteLine("Processing from [" + args[1] + "] to the end");}
  else if(args[2]!="") {Console.WriteLine("Processing from the beggining to [" + args[2] + "]");}
  else {Console.WriteLine("Processing all pages");}

  pages = new PageList(wiki); //create a new (for now, empty) list of pages
  if(args[1]!="" && args[1].Contains(".txt")) {pages.FillFromFile(args[1]);} //fill pages list from the specified text file
  else {pages.FillFromAllPages(args[1]==""?"":args[1], 0, false, int.MaxValue, args[2]==""?"":args[2]);} //or fill it from Special:AllPages
  pages_count = pages.Count();
  page_list_count_str = pages_count.ToString("#,#", culture); //number of pages to process, casted to string

  start_moment = DateTime.Now; //moment of time when the bot started running (needed to calculate remaining time)

  if(pages_count > 2*threads_count) //if there are a lot of pagees, process them in several threads
  {
	//launch the threads
	List<Thread> threads = new List<Thread>(); //list of threads
	for(int i=0; i<threads_count-1 ; i++)
	{
		threads.Add( new Thread( OneThreadOfPageProcessing ) ); //add a new thread to the list
		threads[i].Start(); //launch a new thread
	}
	OneThreadOfPageProcessing(); //one part of programm should be executed in the current thread (in which the programm was started)

	//join the threads
	for(int i=0; i<threads_count-1 ; i++)
	{
		threads[i].Join();
	}
  }
  else {threads_count = 1; OneThreadOfPageProcessing();} //if not a lot, then only in one thread

  Console.WriteLine("\nDONE!"); //when all threads ended running and were jined, write to console that the programm reached the end
 }
}