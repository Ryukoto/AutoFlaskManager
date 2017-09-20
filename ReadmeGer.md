# Einführung

Das ist ein Flask-Verwaltungs Plugin mit vielen Anpassungsmöglichkeiten für POEHUD.  
###### *Anmerkung: Plugins werden nur von der x64 Version von PoeHUD unterstützt.*

# Installation
- Drücke "Clone or Download -> Download Zip" oder über den Reiter "Release" die neueste Version runterladen.
- Entpacke den Ordner aus dem .zip Archiv in den "Plugin" Ordner innerhalb von PoeHUD.
- Starte Path of Exile im 64-Bit Modus.
- Starte PoeHUD im 64-Bit Modus.
- Öffne das PoeHUD Menü -> Plugins -> Aktivire "AutoFlaskManager"
- Deaktiviere die "About" Option, um die Mitwirkenden auszublenden..
- Stelle das Plugin nach deinen Wünschen ein.
- *Beachte: Ein roter Hintergrund deuted auf eine Deaktivierung der Einstellung hin.*

# AutoQuit  
- AutoQuit ist jetzt ein eigenständiges Plugin. (https://github.com/zaafar/AutoQuit)

# F.A.Qs  
###### *Bitte lese bei Problemen zuerst das FAQ durch. Bei weiteren Fragen kannst du Hilfe im offiziellen Discord Kanal Hilfe suchen - [AutoFlaskManager Support](https://discord.gg/sK9JdJH)*
###### *Besteht das Problem weiterhin, kannst du eine Liste von Fehlern auf der zuständigen [AutoFlaskManager Issues](https://github.com/zaafar/AutoQuit/issues) Seite abschicken. (Bitte mit Bildern)*  

```
Wie aktiviere ich offensive Flasks mit den Maustasten?
```
- (Danke an xxsevernajaxx für diese Einstellung)
- Gege in den "AutoFlaskManager" Ordner und öffne die "config.ini" mit Notepadd++ oder Ähnlichem. 
![alt text](http://i.imgur.com/grKGDPP.png "Help Image 1")
- Finde die Einstellung für die offensive Flasknutzung:

![alt text](http://i.imgur.com/m4XLfom.png "Help Image 2")

- Anmerkung: Bei dir könnte da etwas leicht anderes stehen.
- Tausche die Zeilen innerhalb der blauen Klammern mit folgenden Zeilen aus
```
"OffensiveWhenAttacking": false,
"UseWhileKeyPressed": true,
"KeyPressed": {
"Value": 2
```
- Wenn du jetzt die rechte Maustaste im Spiel drückst, sollten die offensiven Flasks genutzt werden. (Auf der Maustaste MUSS ein Skill gelegt sein, sonst funktioniert das nicht)

- Wenn du einen anderen Knopf benutzen willst, kannst du den entsprechenden Zeichenwert hier nachsehen:
http://cherrytree.at/misc/vk.htm
```
Auto Quit ist zu langsam/Auto Quit geht nicht?
```
- Stelle sicher, dass du cports.exe nach _*c:\Windows\System32*_ kopiert hast.
- Nutze den "Predictive Network mode" (_*PoE Optionen->UI->Network Mode*_ muss im Login-Bereich eingestellt werden).
- Stelle sicher, dass du AutoQuit, als auch AutoFlaskManager nach _*\Plugins\*_ entpackt und im Spiel aktiviert hast.
- Schlägt all dies fehl, kannst du einen Bericht anfertigen.
  1. Öffne das PoEHUD Menü. (Drei rote Striche in der oberen linken Eccke)
  2. Vergewissere dich, dass AutoQuit und Flask Manager beide aktiviert sind und überprüfe deren Einstellungen.
  3. Aktiviere den "debug mode" ( _*Flask Manager->UI Settings*_ )
  4. Drücke F4 um Autoquit manuell auszuführen.
  5. Schließe das Spiel und öffne die ErrorLog.txt in _*\Path of Exile\ErrorLog.txt*_
  6. Ist das Problem nicht behebbar, erstelle einen Fehlerbericht mit Bildern (AutoQuit Einstellungen geöffnen, mit dem entsprechenden Fehler) und suche Hilfe auf der [AutoQuit](https://github.com/zaafar/AutoQuit/issues) "Issues" Seite, oder dem Discord [support](https://discord.gg/sK9JdJH) Kanal.
```
Es gibt ein POEHUD/AutoFlaskManager Update, was soll ich machen?
```
Lösche den alten Ordner bevor du die neue Version installierst.  
###### *Du kannst ein Bild von deinen Einstellungen machen, um die Neuinstallation zu vereinfachen.

```
Es gibt ein Path of Exile Update, was soll ich maczhen?
```
Warte auf ein POEHUD Update. Sobald POEHUD funktioniert, werden auch die Plugins funktionieren.

```
Funktioniert dieses Plugin auch mit der 32-Bit Version?
```
Nein, es wird nur die 64-Bit Version unterstützt.

```
Warum unterstützt dieses Plugin einige Funktionen vom Original "AutoAHK" nicht?
```
Weil sie entweder nichts mit Flasks zu tun hat oder ohne Nutzen sind.
Es ist keine Erweiterung geplant. Du kannst aber gerne eine schreiben.

```
Unterstützt dieses Plugin auch andere Sprachen?
```
Jeder ist dazu eingeladen, seine Übersetzung zu veröffentlichen.

```
Mein Plugin geht nicht. (Meine Flasks sind auf andere Tasten gelegt)
```
Ändere den Tastenwert in "flaskbind.json" im _*\Flask Manager\Config\*_ Ordner.

Anhaltspunkte finden sich in _*Documents\My Games\Path of Exile\production_Config.ini*_  
Suche nach diesen Zeilen:
- use_flask_in_slot1=49
- use_flask_in_slot2=50
- use_flask_in_slot3=51
- use_flask_in_slot4=52
- use_flask_in_slot5=53

und ersetze diese Zahlen in flaskbind.json.

```
Flask Manager benutzt meine Flasks viel zu schnell.
```
Du musst die Verzögerung erhöhen. 1000ms = 1 Sekunde  
Versuche diese zahl schrittweise um 250-500ms zu erhöhen, bis es deinen Wünschen entspricht.

```
Was ist der debug mode/debug log. Wie kann ihn benutzen?
```
- Der Debug Modus ist da, um uns nützliche Informationen bei der Hilfe zu geben.
- Der Modus kann innerhalb der AutoFlaskManager Plugin Einstellungen aktiviert werden.
- Dieser Modus erstellt eine autoflaskmanagerdebug.log im Ordner von PoeHUD.
- Diese Datei wird nach der Schließung vom Spiel oder von PoeHUD neu geschrieben.
- Neben dieser Datei werden Bilder von euren Fehlern gerne geshen.

```
Da steht so viel Spam in den Logs.
```
Deaktiviere den Debug Modus.

```
Ich bekomme folgende Warnung: "Warnung: Speed run mod wird auf Mana/Leben/Hybrid Flasks ignoriert." / ("Warning: Speed Run mod is ignored on mana/life/hybrid flasks.")
```
Benutze Alt orb auf den betroffenen Flaks, da Mana/Leben/Hybrid Flasks diese Mod nicht haben sollten.
Benutze andere Flasks für Speed Mods.

```
Das Plugin scheint fehlerhaft zu sein, kann ich helfen?
```
Ja, bitte schicke uns einen Fehlerbericht, sowie Bilder von dem Fehler zu.

---------

# POEHUD im Entwickler Bereich
- Lade die x64 POEHUD Version runter.
 - git clone https://github.com/TehCheat/PoEHUD.git -b x64
- Öffne ExileHUD.sln and wähle "Debug" im oberen Menü aus.
- Drücke Build-all (Build->Build-all). Anmerkung: Das erstellt den erforderlichen Ordner PoEHUD\src\bin\x64\Debug
- Kopiere folgende ordner in den Debug Ordner.
 - config
 - plugins
 - sounds
 - textures
- Gehe in den gerade kopierten Plugin Ordner. 
 - git clone https://github.com/Xcesius/AutoFlaskManager.git
- Öffne "ExileHUD.sln" erneut und rechtsklicke "Solution ExileHUD" -> Add -> Existing Project
	und wähle "FlaskManager.csproj" aus.
- Füge POEHUD Referenzen hinzu.
 - Rechtsklicke FlaskManager/References and wähle "Add References" aus.
 - Wähle "Projects -> PoeHud" aus.
 - Klicke OK
- Fertig.
