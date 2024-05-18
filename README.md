# bl_webserverinfo

- Official Taleworlds documentation
https://moddocs.bannerlord.com/multiplayer/hosting_server/

This plugin is used pure serverside to enable a Controller which returns XML/Json with the current server settings.
For the most players it will be useless but you can use it to fetch the json response of your server to use them on your website or for a Discord bot or something like that.
Tournament admins may also use it to remotely observe player IDs in an ongoing match or check if there are any spectators on a server.

- With the /server_info route you can have a detailed json or XML response of the current server settings.
  `http://YOURSERVERIP:PORT/server_info` - Returns server configuration in a json format
  With the optional query parameter `http://YOURSERVERIP:PORT/server_info?xml=1` you can switch to a XML response if you prefer that.
![image](https://github.com/GerGotha/bl_webserverinfo/assets/1354209/5e07c32f-a7c8-47bf-ad51-e044bb4beae1)
![image](https://github.com/GerGotha/bl_webserverinfo/assets/1354209/afd30402-f0b5-470e-a466-b27082f565f2)



- With the /warband Route you can have the same format as the Warband servers returned. But remember: Warband and Bannerlord have way different configuration options. Therefore some settings are missing, not correct or a bit mixed. For example: Bannerlord has more teamdamage options to disginguish between ranged and melee friendly fire. In this solution it will always return the higher value instead. Also map ids are not really used. Therefore it will only return -1.
  `http://YOURSERVERIP:PORT/server_info/warband` - Returns server configuration in a json format. The `?xml=1` query parameter will once again return XML instead.

![image](https://github.com/GerGotha/bl_webserverinfo/assets/1354209/6d84a783-7bd2-4b60-ae35-515b2cd364df)

- Installation
The installation is quiete simple. Just download the zip directory or build the project on your own. Extract the WebServerInfo directory into your `Mount & Blade II Dedicated Server/Modules` folder.
![image](https://github.com/GerGotha/bl_webserverinfo/assets/1354209/5baa54dd-fcb2-4624-8643-5719fcf4f09c)

After that add the `WebServerInfo` module to your server startup at the end. Example: `_MODULES_*Native*Multiplayer*WebServerInfo*_MODULES_`

If you build the project on your own, do not forget to add `MB_SERVER_PATH` environment variable. `MB_SERVER_PATH` should point to your `Mount & Blade II Dedicated Server` directory.

