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
		<script>
        // conditionizr.com
        // configure environment tests
        conditionizr.config({
            assets: '<?php echo get_template_directory_uri(); ?>',
            tests: {}
        });
        </script>

	</head>
	<body <?php body_class(); ?>>

		<!-- wrapper -->
		<div class="wrapper">

			<!-- header -->
			<header class="header clear" role="banner">

					<!-- nav -->
                <nav class="navbar sticky-top navbar-custom-dark navbar-expand-lg">

                    <a class="navbar-brand" href="#"><img class="ss13-brand-image" src="<?php bloginfo('stylesheet_directory'); ?>/img/ss13_64.png" alt="Site Logo"></a>
                    <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                        <span class="navbar-toggler-icon"></span>
                    </button>
                        <?php html5blank_nav(); ?>
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
					<!-- /nav -->
                <!-- headerImage -->
                <div class="headerImage flex-center" >
                    <div class="pagename">
                        <a>unitystation</a>
                    </div>
                </div>
                <!-- /headerimage -->

            </header>
			<!-- /header -->
