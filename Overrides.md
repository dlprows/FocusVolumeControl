## Overrides

Some games use particularly agressive forms of anti-cheat that interfere with the ability to determine information about the focused application, and then pair it to the appropriate audio process.

Unfortunately, there is nothing I can do about that.

In order to work around this, the overrides mechanism is there for you to set up manual mappings.

I chose to base it off of a process's Main Window Title.

These can change throughout the usage of an application. Once again, there's nothing I can do about that.

The reason I chose to use the Main Window Title despite this problem, is because in the case of some games, it was one of the only pieces of information that I could get about the running process due to its anti-cheat.

There is no way of knowing if this will work in all cases, but it seems to be reliable for the time being. And if I'm ever unable to get the Main Window's Title, I don't know if there will be a different data point to use for matching. Its kind of just the only thing available.

In order to make it so that I don't have to update the plugin for each game using agressive anti-cheat, and to make it so that you don't have to wait for me to fix something, I have created a way to put overrides into the plugin directly.


## Syntax

```
<Match type>: Window title string
audio process string

//lines starting in // are comments, and are ignored
```


```
Match Type
eq: equals - case insensitive
start: starts with - case insensitive
end: ends with - case insensitive
regex: regular expression - case sensitive
```

## Examples

```
//helldivers 2 has a trademark symbol in it, and those are hard to type. 
//so we just find a window that starts with helldivers
start: Helldivers
helldivers
```


```
//you can actually map anything you want. it doesn't have to be only things with anti-cheat problems
eq: task manager
Google Chrome
```

## Help

Getting window titles can be a little hard sometimes. You can mouse over the icon on the start bar, and get it from there.

Another great way to get it is to run this powershell

```
Get-Process | Where-Object ($_.mainWindowTitle} | Format-Table mainWindowTitle
```

For audio processes right click on the volume in the tray of the start bar, and open the Volume Mixer. When you look through the list of apps, you can just type the name from that.

Alternatively you can put in the name of the actual executable. The easiest way to get to those is to run the SoundBrowser published in the releases in github.

