# Hosting a Server

This documents how to run a server, either by hosting + playing or by running headlessly (no GUI).

### Listen server (play and host)
You can host a game (so-called listen server) by checking the box when starting a game, which allows you to host + play. 

If you're using Unity Hub, go to Installations and launch game executable from there. 

### Headless server (host on a dedicated machine)
If you want to dedicate a machine for server hosting you can make game run in "headless" mode, meaning it runs without a UI. 
This mode is sometimes required for reproducing complex issues.
For a headless server, there are a few options:
1. Run game executable from the command line:
   ```
   Unitystation-Server -batchmode -nographics -logfile log1.txt
   ```
2. If you're testing from Unity Editor: Check the "Test Server" box in Managers > GameData in the lobby scene before running (make sure to leave this unchecked in the client build though). You should be able to join using a client once it's started, you won't get any GUI. This is supposed to simulate running a headless server but we aren't 100% confident on it yet. We refer to it as "fake headless"

### Hosting on Google Cloud
If you want to host from a remote server, you can do it via a Google Cloud Compute Engine instance. First of all, create an account or login to an existing one and start using your free 300 dollars credit. Go to Compute Engine section from the left navbar and start a project. Then, inside that project, create an instance. 

![Screenshot_2020-09-04 Google Cloud Platform](https://user-images.githubusercontent.com/64000371/92240826-e4a1d300-eec5-11ea-957a-27d3aafeaa96.png)

![Screenshot_2020-09-04 Google Cloud Platform(1)](https://user-images.githubusercontent.com/64000371/92240837-e8cdf080-eec5-11ea-9fec-8dcedfdfb390.png)

We recommend using Ubuntu 18.04 as the boot disk, and 10 GBs of space should be enough. If your server will host 20 or less players, you should choose first generation N1 for Machine Configuration because it costs less, but server will work slower. 

After your instance is created, search External IPs from the top and go there. 

![Screenshot_2020-09-04 External IP addresses – VPC network – turkniggas – Google Cloud Platform](https://user-images.githubusercontent.com/64000371/92240818-e10e4c00-eec5-11ea-823e-8547e2e898b8.png)

Select Static and give it a name. Then, select Firewall from the left bar. This is where we will portforward. Add a new firewall rule and apply the settings below:

![Screenshot_2020-09-04 Firewall rule details – VPC network – turkniggas – Google Cloud Platform](https://user-images.githubusercontent.com/64000371/92240791-d94ea780-eec5-11ea-8355-32ac99fc1ee2.png)

You also need to do it for egress, so repeat the steps and change the ingress to egress. 

Now that you have a static ip and open ports, go [here](https://www.yougetsignal.com/tools/open-ports/) and check the ports 7777 and 5555. If the ports are open, you almost did it. Now, return to your Cloud Engine Instances page and click the SSH button. This will connect you to your server via command-line. If you can enter inputs(Try writing "ls" for example), copy and paste this code:

**sudo apt update && wget -O installserver.sh https://unitystationfile.b-cdn.net/installserver.sh && sudo chmod +x installserver.sh && sudo bash installserver.sh**

After saying yes to every prompt, you will see the setup screen. Fill it as you like.

![resim](https://user-images.githubusercontent.com/64000371/92241244-96d99a80-eec6-11ea-8180-073de6a7b4bd.png)

Congratulations! Your server is good to go. Just copy your static ip and connect to it. 



