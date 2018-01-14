<!DOCTYPE html>
<html lang="en">

<head>

  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="Shamelessly porting Space Station 13 to Unity">
  <meta name="author" content="">
    <link rel="apple-touch-icon" sizes="180x180" href="/assets/img/favicons/apple-touch-icon.png">
    <link rel="icon" type="image/png" sizes="32x32" href="/assets/img/favicons/favicon-32x32.png">
    <link rel="icon" type="image/png" sizes="192x192" href="/assets/img/favicons/android-chrome-192x192.png">
    <link rel="icon" type="image/png" sizes="16x16" href="/assets/img/favicons/favicon-16x16.png">
    <link rel="manifest" href="/assets/img/favicons/manifest.json">
    <link rel="mask-icon" href="/assets/img/favicons/safari-pinned-tab.svg" color="#224ce0">
    <link rel="shortcut icon" href="/assets/img/favicons/favicon.ico">
    <meta name="apple-mobile-web-app-title" content="Unitystation">
    <meta name="application-name" content="UnityStation">
    <meta name="msapplication-TileColor" content="#2b5797">
    <meta name="msapplication-TileImage" content="/assets/img/favicons/mstile-144x144.png">
    <meta name="msapplication-config" content="/assets/img/favicons/browserconfig.xml">
    <meta name="theme-color" content="#ffffff">
  <meta property="og:title" content="Unitystation" />
  <meta property="og:type" content="Website" />
  <meta property="og:url" content="http://unitystation.org/" />
  <meta property="og:image" content="http://unitystation.org/assets/img/ian.jpg" />
  <title>Unitystation</title>
    <style>:root{--blue:#007bff;--indigo:#6610f2;--purple:#6f42c1;--pink:#e83e8c;--red:#dc3545;--orange:#fd7e14;--yellow:#ffc107;--green:#28a745;--teal:#20c997;--cyan:#17a2b8;--white:#fff;--gray:#868e96;--gray-dark:#343a40;--primary:#007bff;--secondary:#868e96;--success:#28a745;--info:#17a2b8;--warning:#ffc107;--danger:#dc3545;--light:#f8f9fa;--dark:#343a40;--breakpoint-xs:0;--breakpoint-sm:576px;--breakpoint-md:768px;--breakpoint-lg:992px;--breakpoint-xl:1200px;--font-family-sans-serif:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif,"Apple Color Emoji","Segoe UI Emoji","Segoe UI Symbol";--font-family-monospace:"SFMono-Regular",Menlo,Monaco,Consolas,"Liberation Mono","Courier New",monospace}*,::after,::before{box-sizing:border-box}html{font-family:sans-serif;line-height:1.15;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;-ms-overflow-style:scrollbar}@-ms-viewport{width:device-width}nav{display:block}body{margin:0;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif,"Apple Color Emoji","Segoe UI Emoji","Segoe UI Symbol";font-size:1rem;font-weight:400;line-height:1.5;color:#212529;text-align:left;background-color:#fff}h1,h3{margin-top:0;margin-bottom:.5rem}p{margin-top:0;margin-bottom:1rem}ul{margin-top:0;margin-bottom:1rem}a{color:#007bff;text-decoration:none;background-color:transparent;-webkit-text-decoration-skip:objects}img{vertical-align:middle;border-style:none}a,button,input:not([type=range]){-ms-touch-action:manipulation;touch-action:manipulation}button{border-radius:0}button,input{margin:0;font-family:inherit;font-size:inherit;line-height:inherit}button,input{overflow:visible}button{text-transform:none}button,html [type=button]{-webkit-appearance:button}[type=button]::-moz-focus-inner,button::-moz-focus-inner{padding:0;border-style:none}::-webkit-file-upload-button{font:inherit;-webkit-appearance:button}h1,h3{margin-bottom:.5rem;font-family:inherit;font-weight:500;line-height:1.2;color:inherit}h1{font-size:2.5rem}h3{font-size:1.75rem}.container{width:100%;padding-right:15px;padding-left:15px;margin-right:auto;margin-left:auto}@media (min-width:576px){.container{max-width:540px}}@media (min-width:768px){.container{max-width:720px}}@media (min-width:992px){.container{max-width:960px}}@media (min-width:1200px){.container{max-width:1140px}}.row{display:-ms-flexbox;display:flex;-ms-flex-wrap:wrap;flex-wrap:wrap;margin-right:-15px;margin-left:-15px}.col-md-6{position:relative;width:100%;min-height:1px;padding-right:15px;padding-left:15px}@media (min-width:768px){.col-md-6{-ms-flex:0 0 50%;flex:0 0 50%;max-width:50%}}.collapse{display:none}.nav-link{display:block;padding:.5rem 1rem}.navbar{position:relative;display:-ms-flexbox;display:flex;-ms-flex-wrap:wrap;flex-wrap:wrap;-ms-flex-align:center;align-items:center;-ms-flex-pack:justify;justify-content:space-between;padding:.5rem 1rem}.navbar-brand{display:inline-block;padding-top:.3125rem;padding-bottom:.3125rem;margin-right:1rem;font-size:1.25rem;line-height:inherit;white-space:nowrap}.navbar-nav{display:-ms-flexbox;display:flex;-ms-flex-direction:column;flex-direction:column;padding-left:0;margin-bottom:0;list-style:none}.navbar-nav .nav-link{padding-right:0;padding-left:0}.navbar-collapse{-ms-flex-preferred-size:100%;flex-basis:100%;-ms-flex-positive:1;flex-grow:1;-ms-flex-align:center;align-items:center}.navbar-toggler{padding:.25rem .75rem;font-size:1.25rem;line-height:1;background:0 0;border:1px solid transparent;border-radius:.25rem}.navbar-toggler-icon{display:inline-block;width:1.5em;height:1.5em;vertical-align:middle;content:"";background:no-repeat center center;background-size:100% 100%}@media (min-width:992px){.navbar-expand-lg{-ms-flex-flow:row nowrap;flex-flow:row nowrap;-ms-flex-pack:start;justify-content:flex-start}.navbar-expand-lg .navbar-nav{-ms-flex-direction:row;flex-direction:row}.navbar-expand-lg .navbar-nav .nav-link{padding-right:.5rem;padding-left:.5rem}.navbar-expand-lg .navbar-collapse{display:-ms-flexbox!important;display:flex!important;-ms-flex-preferred-size:auto;flex-basis:auto}.navbar-expand-lg .navbar-toggler{display:none}}.flex-row{-ms-flex-direction:row!important;flex-direction:row!important}@supports ((position:-webkit-sticky) or (position:sticky)){.sticky-top{position:-webkit-sticky;position:sticky;top:0;z-index:1020}}.sr-only{position:absolute;width:1px;height:1px;padding:0;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;-webkit-clip-path:inset(50%);clip-path:inset(50%);border:0}.p-2{padding:.5rem!important}@media (min-width:768px){.ml-md-auto{margin-left:auto!important}}.text-center{text-align:center!important}.fab,.far{-moz-osx-font-smoothing:grayscale;-webkit-font-smoothing:antialiased;display:inline-block;font-style:normal;font-variant:normal;text-rendering:auto;line-height:1}.fa-facebook:before{content:"\f09a"}.fa-github:before{content:"\f09b"}.fa-patreon:before{content:"\f3d9"}.fa-play-circle:before{content:"\f144"}.fa-reddit-alien:before{content:"\f281"}.fa-twitter:before{content:"\f099"}.fa-youtube:before{content:"\f167"}.sr-only{border:0;clip:rect(0,0,0,0);height:1px;margin:-1px;overflow:hidden;padding:0;position:absolute;width:1px}.fab{font-family:Font Awesome\ 5 Brands}.far{font-weight:400}.far{font-family:Font Awesome\ 5 Free}@font-face{font-family:'Montserrat';font-style:normal;font-weight:400;src:local('Montserrat Regular'),local('Montserrat-Regular'),url(https://fonts.gstatic.com/s/montserrat/v12/zhcz-_WihjSQC0oHJ9TCYC3USBnSvpkopQaUR-2r7iU.ttf) format('truetype')}body{font-family:'Montserrat',sans-serif;font-weight:400;font-size:16px;background-color:#efefef;-webkit-font-smoothing:antialiased;-webkit-overflow-scrolling:touch;overflow-x:hidden}.flex-center{display:flex;align-items:center;justify-content:center}.ss13-brand-image{width:48px;height:48px;margin-right:15px}.navbar-brand{font-weight:700;font-size:2rem;color:white!important}.navbar-custom-dark{background-color:#2f2f2f}.navbar-custom-dark a{color:#aaa}.videoWrapper{position:relative;width:100%;height:100%;margin:0 20%}.videoWrapper iframe{position:absolute;border:0;top:0;left:0;width:100%;height:100%}@media screen and (min-width:401px){.playButton{color:red;font-size:11rem}}@media screen and (max-width:400px){.playButton{color:red;font-size:8rem}}@media screen and (min-width:581px){.closePlayer{position:absolute;left:1%;color:#aaa;text-align:center;border:2px solid #aaa;padding:5px}}@media screen and (max-width:580px){.closePlayer{display:none;visibility:hidden}}@media screen and (min-width:581px){.closePlayerMob{display:none;visibility:hidden}}@media screen and (max-width:580px){.closePlayerMob{position:absolute;left:1%;color:#aaa;text-align:center;border:2px solid #aaa;padding-top:5px;padding-bottom:5px;padding-right:10px;padding-left:10px}}@media (min-width:992px){.container{max-width:940px}}@media (min-width:768px) and (max-width:991px){.container{max-width:720px}}@media (max-width:767px){.container{max-width:540px}}@media screen and (min-width:871px){.headerImage{background:url(../img/header-min.png) no-repeat center top;background-attachment:relative;background-position:center center;height:300px;background-size:cover}}@media screen and (max-width:870px) and (min-width:396px){.headerImage{background:url(../img/headerlow-min.png) no-repeat center top;background-attachment:relative;background-position:center center;height:210px;background-size:cover}}@media screen and (max-width:395px){.headerImage{background:url(../img/headermob-min.png) no-repeat center top;background-attachment:relative;background-position:center center;height:150px;background-size:cover}}.about-image{width:200px}.ourProject{font-size:2.5rem;margin:2rem 0}</style> <!-- Bootstrap core CSS -->

  <!-- Font Awesome Icons -->


</head>

<body>

  <!-- START NAV -->
  <nav class="navbar sticky-top navbar-custom-dark navbar-expand-lg">
    <a class="navbar-brand" href="#"><img class="ss13-brand-image" src="assets/img/ss13_64.png" alt="Site Logo">Unitystation</a>
    <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarSupportedContent">
      <ul class="navbar-nav">
        <li class="nav-item">
          <a class="nav-link" href="#about">About</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" href="#download">Download</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" href="#contact">Contact</a>
        </li>
      </ul>
    </div>
    <ul class="navbar-nav flex-row ml-md-auto d-none d-md-flex">
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-github" href="https://github.com/unitystation/unitystation" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-github"></i>
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-patreon" href="https://www.patreon.com/unitystation" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-patreon"></i>
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-twitter" href="https://twitter.com/UnityStation" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-twitter"></i>
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-facebook" href="https://www.facebook.com/UnityStation13/" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-facebook"></i>
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-reddit" href="https://www.reddit.com/r/unitystation/" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-reddit-alien"></i>
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link p-2 socialMedia-youtube" href="https://www.youtube.com/channel/UCBfgfy7t3VOI5nsgkR5k4Tw" target="_blank" rel="noopener" aria-label="GitHub">
          <i class="fab fa-youtube"></i>
        </a>
      </li>
    </ul>
  </nav>
  <!-- END NAV -->

  <!-- START HERO IMAGE / EMBEDDED VIDEO -->
  <div class="headerImage flex-center" >
    <div class="videoWrapper" style="display:none">
      <iframe sandbox="allow-scripts allow-same-origin allow-same-origin allow-presentation" id="embeddedVideo" width="420" height="315">
      </iframe>
    </div>
    <div class="closePlayer" style="display: none;" onclick="closeVideo()">
      Close Player
    </div>
      <div class="closePlayerMob" style="display: none;" onclick="closeVideo()">
       X
      </div>
    <div class="playButton" onclick="playVideo()">
      <i class="far fa-play-circle"></i>
    </div>
  </div>

  <!-- END HERO IMAGE / EMBEDDED VIDEO -->

  <!-- START ABOUT-->


  <!-- our project container -->
  <div class="container" id="about">
    <div class="flex-center">
      <img class="about-image" src="assets/img/about.png" alt="About Text">
    </div>
    <h1 class="sr-only">About</h1>
    <h3 class="ourProject text-center"></h3>
      <noscript>Your browser does not support JavaScript. Our si</noscript>
    <div class="row">
      <div class="col-md-6">
        <p>
          Space Station 13, the greatest clown simulator ever created, or atmos simulator? Who knows, but if you have ever played SS13 before you can agree on one thing and that is that BYOND sucks! This is where Unitystation comes in. Started in November 2016
          Unitystation has endeavored to ensure SS13 has a long and prosperous future outside of Byond by cloning the /TG/ source to Unity.
        </p>

        <p>
          It has a fully working net framework, a dedicated server, map editor, inventory, basic interactions, all the items and clothes from the TG branch, weapons, and damage. The project has a growing and dedicated community of contributors who are eager to see this thing become a reality. Best of all it
          is all open source and <a href="https://github.com/unitystation/unitystation">available on GitHub</a>.
        </p>
      </div>

      <div class="col-md-6">
        <p>
          But the journey to Version 1.0 is still a long one and this is where patreon comes in. Funds raised will be used to place 'Bounties' on each TODO ticket that will provide an incentive for contributors to complete work a lot sooner then what would of occurred.
          We call this the <strong>Nanotrasen Bounty System</strong>. Code submitted for each ticket will be peer reviewed before the bounty is awarded. On top of this patrons will have a say in the development of Unitystation with larger donors
          having more power. Having the NBS system means Unitystation can achieve version 1.0 at lightning speed while also rewarding those who take the time out to contribute!
        </p>

        <p>
          All transactions will be made visible and public on our <a href="https://discord.gg/tFcTpBp">discord channel</a>.
        </p>
      </div>
    </div>
  </div>

  <!-- patreon container -->
  <div class="patreon-container">
    <div class="container">
      <div class="row">
        <div class="col-md-12 text-center margin-top-30">
          <h4 class="margin-bottom-1rem">Support this open source project today and <a href="https://www.patreon.com/unitystation">Become a Patron</a>.</h4>
          <a id="patron-button" href="https://www.patreon.com/unitystation">
              <img id="patron-button-white" src="assets/img/become_a_patron_button-white.png" alt="Become a Patron-white">
              <img id="patron-button-color" src="assets/img/become_a_patron_button.png" style="display:none;" alt="Become a Patron">
            </a>
        </div>
      </div>
    </div>
  </div>
  <!-- END ABOUT -->

  <!-- START DOWNLOAD -->
  <div class="download-container" id="download">
    <div class="honk" id="honk">
    </div>
    <audio preload="none" id="audio" src="assets/audio/Clown.aac"></audio>
    <button class="audio-button" onclick="toggleAudio()" type="button">
      <i id="audio-button-icon" class="fas fa-volume-off"></i>
    </button>
    <div class="container download-container-inner">
      <div class="flex-center">
        <img class="download-image" src="assets/img/download.png" alt="Download text">
      </div>
      <h1 class="sr-only">Download</h1>
      <div class="version-container">
        <p>v0.1.3 - &nbsp;</p>
        <p>July 15, 2017</p>
      </div>
      <div class="version-container">
        <p>[Depreciated]</p>
      </div>
      <div class="row button-container">
        <div class="col-md-4 flex-center margin-bottom-1rem">
          <a class="btn btn-lg btn-primary" href="https://github.com/unitystation/unitystation/releases/download/0.1.3/windows.zip" role="button">
            <i class="fab fa-windows"></i> Windows
          </a>
        </div>
        <div class="col-md-4 flex-center margin-bottom-1rem">
          <a class="btn btn-lg btn-primary" href="https://github.com/unitystation/unitystation/releases/download/0.1.3/osx.zip" role="button">
            <i class="fab fa-apple"></i> OSX
          </a>
        </div>
        <div class="col-md-4 flex-center">
          <a class="btn btn-lg btn-primary" href="https://github.com/unitystation/unitystation/releases/download/0.1.3/linux.zip" role="button">
            <i class="fab fa-linux"></i> Linux
          </a>
        </div>
      </div>
    </div>
  </div>
  <!-- END DOWNLOAD -->

  <!-- START CONTACT -->
  <div class="flex-center" id="contact">
    <img class="contact-image" src="assets/img/contact.png" alt="Contact text">
  </div>
  <h1 class="sr-only">Contact</h1>
  <div class="container-narrow margin-top-30 margin-bottom-30" style="margin-bottom: 125px" id="emailForm">
    <?php
$action=$_REQUEST['action'];
if ($action=="")    /* display the contact form */
    {
    ?>
    <form  action="/#contact" method="POST" enctype="multipart/form-data" >
      <input type="hidden" name="action" value="submit">

      <div class="form-group">
        <label for="inputEmail">Email address</label>
        <input type="email" name="email" class="form-control orange-form-box" id="inputEmail" aria-describedby="emailHelp" placeholder="Enter email">
      </div>
      <div class="form-group">
        <label for="inputName">Name</label>
        <input type="text" name="name" class="form-control orange-form-box" id="inputName" placeholder="Name">
      </div>
      <div class="form-group">
        <label for="textArea">Comments</label>
        <textarea name="message"  class="form-control orange-form-box" id="textArea" rows="3" placeholder="Leave a comment..."></textarea>
      </div>
      <button type="submit" value="Send email" class="btn btn-primary">Submit</button>
    </form>


  <?php
    }
else                /* send the submitted data */
    {
    $name=$_REQUEST['name'];
    $email=$_REQUEST['email'];
    $message=$_REQUEST['message'];
    $sender='noreply@unitystation.org';
    $sendernick='Unitystation Website';
    if (($name=="")||($email=="")||($message==""))
        {
		echo "<p class=\"flex-center\" style=\"font-size:1.5rem;\">ERROR</p>.";
  }
  else{

  $headers .= "Reply-To: $name <$email>\r\n";
  $headers .= "Return-Path: $name <$email>\r\n";
  $headers .= "From: $sendernick <$sender>\r\n";
  $headers .= "Organization: $sendernick\r\n";
  $headers .= "MIME-Version: 1.0\r\n";
  $headers .= "Content-type: text/plain; charset=iso-8859-1\r\n";
  $headers .= "X-Priority: 3\r\n";
  $headers .= "X-Mailer: PHP". phpversion() ."\r\n";
  mail("info@unitystation.org", "New form submission", "$message.", $headers);



      echo "    <p class=\"flex-center\" style=\"font-size:2rem;\">Thank you! <br /></p>
    <p class=\"flex-center\" style=\"font-size:1.5rem;\">Your email has been sent.</p>";
  }
  echo "  </div>";
  }
  ?>

  </div>
  <!-- END CONTACT -->


  <!-- START FOOTER -->
  <div class="footer-container">
    <div class="container">
      <div class="flex-center footerBrand">

      </div>
      <div class="row centered footerBottomRow">
        <div class="col-lg-4 flex-center">
            <img  src="assets/img/built-with-resentment.svg" alt="Build with Resentment">
           </div>
           <div class="col-lg-4 flex-center">
               <a class="navbar-brand" href="#"><img class="ss13-brand-image" src="assets/img/ss13_64.png" alt="Footer Logo"></a>
           </div>
           <div class="col-lg-4 flex-center">
               <img src="assets/img/contains-technical-debt.svg" alt="Contains Technical Dept">
              </div>
            </div>
          </div>
        </div>

        <!-- Bootstrap core JavaScript -->
  <link href="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-beta.2/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://use.fontawesome.com/releases/v5.0.2/css/all.css" rel="stylesheet">
  <link href="https://fonts.googleapis.com/css?family=Montserrat" rel="stylesheet">
  <link href="assets/css/style.min.css" rel="stylesheet">
  <script src="https://code.jquery.com/jquery-3.2.1.min.js" integrity="sha256-hwg4gsxgFZhOsEEamdOYGBf13FyQuiTwlAQgxVSNgt4" crossorigin="anonymous"></script>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.12.9/umd/popper.min.js" integrity="sha384-ApNbgh9B+Y1QKtv3Rn7W3mgPxhU9K/ScQsAP7hUibX39j7fakFPskvXusvfa0b4Q" crossorigin="anonymous"></script>
  <script src="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-beta.3/js/bootstrap.min.js" integrity="sha384-a5N7Y/aK3qNeh15eJKGWxsqtnX/wWdSZSKp+81YjTmS15nvnvxKHuzaWwXHDli+4" crossorigin="anonymous"></script>
  <script src="assets/js/honk.min.js"></script>

</body>

</html>
