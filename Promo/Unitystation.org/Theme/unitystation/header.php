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

		<!-- wrapper -->
		<div class="wrapper">

			<!-- header -->
			<header class="header clear" role="banner">

					<!-- nav -->
                <nav class="navbar navbar-custom-dark navbar-expand-lg uppernav">

                    <a class="navbar-brand" href="#"><img class="ss13-brand-image" src="https://unitystation.org/wp-content/uploads/ss13_64.png" alt="Site Logo"></a>
                    <button class="navbar-toggler hidden-md-up pull-left" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                        <span class="navbar-toggler-icon"><i class="fa fa-bars" aria-hidden="true"></i></span>
                    </button>
										<div id="navbarSupportedContent" class="collapse navbar-collapse">
                      <?php unitystation_navMenu(); ?>
	                    <ul class="navbar-nav flex-row flex-center socialMedia-container">
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
									</div>
                </nav>
					<!-- /nav -->
                <!-- headerImage -->
                <div class="headerImage flex-center">
                    <div class="pagename">
                        <a class="flex-center">UNITYSTATION</a>
												<div class="flex-center small-header-text">SS13 REMAKE</div>
                    </div>
                    <div class="navbar navbar-fixed-top navbar-custom-dark navbar-expand-lg lowernav">
                        <?php unitystation_pageMenu(); ?>
                        <?php unitystation_userMenu(); ?>
                    </div>
                </div>
                <!-- /headerimage -->

            </header>
<!-- /header -->
<!-- loginModal -->
						<div class="modal fade" id="loginModal" tabindex="-1" role="dialog" aria-labelledby="loginModalLabel" aria-hidden="true">
							<div class="modal-dialog" role="document">
								<div class="modal-content">
									<div class="modal-header">
										<h5 class="modal-title" id="loginModalLabel"><i class="fa fa-sign-in-alt"></i> Login</h5>
										<button type="button" class="close" data-dismiss="modal" aria-label="Close">
											<span aria-hidden="true">&times;</span>
										</button>
									</div>
									<div class="modal-body">

                                        <?php echo do_shortcode('[user_registration_login]') ?>
                                        <?php// echo do_shortcode('[TheChamp-Login]') ?>
                                        <?php  do_action( 'wordpress_social_login' ); ?>
                                        <?php // echo do_shortcode('[wppb-recover-password]') ?>

                                    </div>
									<div class="modal-footer">
										<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
									</div>
								</div>
							</div>
						</div>

<!-- new user modal -->
						<div class="modal fade" id="newUserModal" tabindex="-1" role="dialog" aria-labelledby="newUserModalLabel" aria-hidden="true">
							<div class="modal-dialog" role="document">
								<div class="modal-content">
									<div class="modal-header">
										<h5 class="modal-title" id="newUserModalLabel"><i class="fas fa-user-plus"></i> New User</h5>
										<button type="button" class="close" data-dismiss="modal" aria-label="Close">
											<span aria-hidden="true">&times;</span>
										</button>
									</div>
									<div class="modal-body">
                                        <?php echo do_shortcode('[user_registration_form id="154"]') ?>
                                        <?php// echo do_shortcode('[TheChamp-Login]') ?>
                                        <?php  do_action( 'wordpress_social_login' ); ?>
									</div>
									<div class="modal-footer">
										<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
									</div>
								</div>
							</div>
						</div>


            <!-- logout modal -->

            <div class="modal fade" id="logoutModal" tabindex="-1" role="dialog" aria-labelledby="logoutModalLabel" aria-hidden="true">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="logoutModalLabel"><i class="fas fa-sign-out-alt"></i> Logout</h5>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div class="modal-body">

                        <?php echo do_shortcode('[logout_to_home text="Logout" class="]') ?>

                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- addUser modal -->
            <div class="modal fade" id="addUserModal" tabindex="-1" role="dialog" aria-labelledby="addUserModalLabel" aria-hidden="true">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="addUserModalLabel"><i class="fas fa-user-plus"></i> New User</h5>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div class="modal-body">
                            <?php echo do_shortcode('[user_registration_form id="154"]') ?>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
