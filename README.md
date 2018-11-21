# Copy Database
A Simple Tool To Copy a Database from one MsSQL Server to Another

## Installation Instructions

1) The Copy database Tool can be downloaded from the releases section [CopyDatabase.msi v1.0.0](https://github.com/Kimahari/CopyDatabase/releases/download/1.0.0/CopyDatabase.msi)

2) Once the installtion process is compeleted you will have access to the Application via Windows start menu.

## Using the application

1) Once the application have been started you will see the following window.

![alt text](Resources/ApplicationStart.png)

* Source : Source Database Server where you whould like to copy one or more databases from
* Destination : Destination Database server where you whould like to copy one or more databases too.
* Load Databases : Loads the Databases from the source Database server
* Wrench Buttons : Advanced Configuration for the Database server sources (User name passwords)

2) Once The databases have been loaded the user will have access to select one or more database to copy from one source to another.

![alt text](Resources/ApplicationLoaded.png)

3) To initialize the copy process click the copy database command at the bottom of the screen **Note** - The databases in the destination server gets dropped once the copy process is started *Please ensure to backup all destination databases before hand...*.

![alt text](Resources/ApplicationCopyStart.png)

# Known Issues

1) Application Crash when Load database is clicked with one or more unconfigured / invalid server sources
2) Copy Database button is not disabled when copy start.
3) Stored proceduces and views fail to copy over when there are references to tables that do not exists (need a continue on failure for these scenarios) .