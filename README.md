# wikipedia_bots

== DESCRIPTION ==

Here is a set of programs (bots) doing maintenance tasks in Wikipedia: they iterate over each page fixing links and references. Bots are written in C# using DotNetWikiBot library (<http://dotnetwikibot.sourceforge.net/>). Supposed to be run on Windows.

Bots can be used in any language version of Wikipedia and in any of its sister projects, but Wikidata (due to that editing there works differently). It is strongly recommended that you use a separate account for automated edits (usually, with username being the username of your base account + a suffix "Bot"). Also, read bot policy of the project, where you will be launching the bot (<https://www.wikidata.org/wiki/Q4048867>). Those policies always require you to get a bot flag to operate. Majority of the projects allow a small test run without a bot flag, then you need to apply for bot flag and, after public discussion, you will get one. A policy can be more loose or more strict, depending on project, with policies of English Wikipedia, German Wikipedia, Dutch Wikipedia and Wikimedia Commons being among the most strict.

Programs were tested and ran multiple times in Ukrainian Wikipedia with no serious problems, but still, it is better to check bots contribution from time to time.

Bots run in multiple threads (unless there are not many pages to process). There will be as many threads, as many cores your CPU has.

== WHAT DOES IT DO ==

* duplicate_references_remover - looks for sets of completely identical references on a page, and if found, leaves only one of them with full text and replaces other references with links to the first. If the page had something like "< ref>Lorem ipsum< /ref> dolor sit amet < ref>Lorem ipsum< /ref> consectetur adipiscing elit < ref>Lorem ipsum< /ref>", it will be replaced with "< ref name=":1">Lorem ipsum< /ref> dolor sit amet < ref name=":1"/> consectetur adipiscing elit < ref name=":1"/>". Each set of identical references is resolved in a separate edit. Example: <https://uk.wikipedia.org/w/index.php?curid=2326131&diff=prev&oldid=31605703>.
* fbclid_remover - removes 'fbclid' and 'igshid' tokens from links (Facebook and Instagram attach those tokens to links shared in those services, either in posts or in messages, to help sites track where the user came from). Those tokens are not useful in any way, but they clutter source code and help track Wikipedia readers. Example: <https://be.wikipedia.org/w/index.php?curid=551442&diff=prev&oldid=3850551>.
* percent_decoder - if there are non-ASCII characters in a URL, many browser percent-encode them (<https://en.wikipedia.org/wiki/Percent-encoding>), for example a whitespace is encoded as "%20". However, all modern browsers can understand non-ASCII characters in URLs without percent-encoding. Links with percent encoding in a Wikipedia page can make this link or paragraph of text, where this link is written, non-human-readable or even break the page layout. Example: <https://uk.wikipedia.org/w/index.php?title=curid=3497460&diff=prev&oldid=31673057>.
* wiki_external_links_correction - turns links to other Wikipedia articles, written as external links, into internal links, like "[https://en.wikipedia.org/wiki/Robot Bot]" into "[[Robot|Bot]]". Example: <https://uk.wikipedia.org/w/index.php?curid=2959167&diff=prev&oldid=31806797>.

== HOW TO USE IT ==

=== Preparation ===

All bots are already compiled into 'exe' files. Those files can be executed without installation of any additional software (they are "out of a box"). However, first you will need to write your username and password in 'login_data.txt' (each in a separate row), and provide data in 'bat' file for the bot.

In 'bat' file (named like "run_taskname.bat") there is a line with call of the 'exe' file, followed by 'args' parameters in quotes separated by whitespaces. In the first parameter there must be an URL of the project where you will be working: language, project name, 'org', separated by dots, e.g. "en.wikipedia.org". The fourth parameter is the edit comment, which will be used when saving changes to the pages.

Second and third parameters set which pages should be processed. You can pass an address of 'txt' file as a second parameter, in this case all pages listed in the file (the file should consist of page names separated by newlines). Otherwise, all articles from one set in the second parameter to the one set in the third parameter will be processed. Empty second parameter means "from the very beginning" and empty third parameter means "to the very end". If both of those parameters are empty (each parameter is just two consecutive quote signs), then all articles exiting on the project will be processed (however, it can take days to process all of them, if there are more than a million articles on the project).

For the 'bat' file to work properly with non-Latin characters, set needed charset in the first line using "ChCp" command, e.g. "ChCp 1251" for Eastern Slavic Cyrillic characters.

However, if you want to make a change in the source code ('cs' files), to apply your changes you need to-recompile the program. For this you need Microsoft .NET Framework to be installed. Make sure that "PATH" variable in your registry links to the Microsoft .NET Framework folder, or otherwise put a link to it in a first line of compile 'bat' file, e.g. "path=C:\Windows\Microsoft.NET\Framework\v3.5\".

To compile the program, launch the needed compile 'bat' file (named like "compile_taskname.bat"). It will list all syntax errors, if there are any. If there are errors, correct them and try again. If the 'bat' file was executed and no syntax errors were listed, then the program was re-compiled.

Make sure that 'bat' files and files with article lists are saved in ANSI encoding, if there are any non-Latin characters (in Notepad++ there is a dropdown menu "Encoding" in the upper bar, fifth from the left).

=== Run ===

To run a bot, launch a corresponding 'bat' file (named like "run_taskname.bat"). A black Windows console will appear, where the progress of execution will be printed. First, it will log into the account, username and password of which were written in 'login_data.txt'. Then it will load a list of pages which you need to process. If there are several thousands [or even millions] of page names to be processed, this will take a [very] long time. After that it will load each page one by one, check if it needs to be changed, and if needed, make changes to the text and save the new version of a page. Console shows an estimatation of how long the programm needs to work to process all pages. When all pages were processed, a word "DONE" will be written in the console.

If any exceptions (errors) occur during execution of a program, they will be logged in the 'errors_log.txt' file (will be created automatically if absent). All exceptions during page load and page save are handled, so if an exception happens during load or save, it will not crash the program.

== CONTACT ==

The bots were developed by User:Tohaomg (Anton Obozhyn). If you have any comments, suggestions, questions or bug reports, please contact the developer with any of listed means:
* anton.obozhyn@wikimedia.org.ua
* https://m.me/tohaomg
* https://t.me/tohaomg
* skype:tohaomg
* https://meta.wikimedia.org/wiki/User_talk:Tohaomg
