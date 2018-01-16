<!doctype html>
<html <?php language_attributes(); ?> class="no-js">
<head>
    <meta charset="<?php bloginfo('charset'); ?>">
    <title><?php wp_title(''); ?><?php if(wp_title('', false)) { echo ' :'; } ?> <?php bloginfo('name'); ?></title>

    <link href="//www.google-analytics.com" rel="dns-prefetch">
    <link href="<?php echo get_template_directory_uri(); ?>/img/icons/favicon.ico" rel="shortcut icon">
    <link href="<?php echo get_template_directory_uri(); ?>/img/icons/touch.png" rel="apple-touch-icon-precomposed">

    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="<?php bloginfo('description'); ?>">

    <?php wp_head(); ?>

</head>
<?


?>


<body <?php body_class(); ?>>

<!-- START NAV -->
<nav class="navbar sticky-top navbar-custom-dark navbar-expand-lg">
    <a class="navbar-brand" href="#"><img class="ss13-brand-image" src="https://unitystation.org/wp-content/uploads/ss13_64.png" alt="Site Logo">Unitystation</a>
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
<div class="landingHeaderImage flex-center" >
    <div class="landingVideoWrapper" style="display:none">
        <iframe sandbox="allow-scripts allow-same-origin allow-same-origin allow-presentation" id="landingEmbeddedVideo" width="420" height="315">
        </iframe>
    </div>
    <div class="landingClosePlayer" style="display: none;" onclick="landingCloseVideo()">
        Close Player
    </div>
    <div class="landingClosePlayerMob" style="display: none;" onclick="landingCloseVideo()">
        X
    </div>
    <div class="landingPlayButton" onclick="landingPlayVideo()">
        <i class="far fa-play-circle"></i>
    </div>
</div>

<!-- END HERO IMAGE / EMBEDDED VIDEO -->
