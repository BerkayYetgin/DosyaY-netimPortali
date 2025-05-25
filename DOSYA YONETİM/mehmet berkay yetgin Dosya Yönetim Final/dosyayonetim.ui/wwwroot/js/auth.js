$(document).ready(function() {
    // Configure Toastr
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Handle login form submission
    $('#loginForm').submit(function(e) {
        e.preventDefault();
        
        const loginData = {
            username: $('#loginUsername').val(),
            password: $('#loginPassword').val()
        };

        $.ajax({
            url: API_SETTINGS.BASE_URL + API_SETTINGS.ENDPOINTS.AUTH.LOGIN,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(loginData),
            success: function(response) {
                if (response.isSuccess) {
                    // Store token in localStorage
                    localStorage.setItem('token', response.token);
                    localStorage.setItem('user', JSON.stringify(response.user));

                    // Admin kontrolü (örnek: user.role veya user.isAdmin)
                    let isAdmin = false;
                    if (response.user.role && response.user.role.toLowerCase() === 'Admin') isAdmin = true;
                    if (response.user.isAdmin === true) isAdmin = true;
                    if (response.user.username && response.user.username.toLowerCase() === 'Admin') isAdmin = true;

                    if (isAdmin) {
                        toastr.success('Hoş geldiniz! Yönlendiriliyorsunuz...', 'Admin Girişi', {
                            timeOut: 1500,
                            onHidden: function() {
                                window.location.href = '/Admin/Index';
                            }
                        });
                        return;
                    }

                    toastr.success('Hoş geldiniz! Giriş başarılı.', 'Başarılı', {
                        timeOut: 2000,
                        onHidden: function() {
                            window.location.href = '/';
                        }
                    });
                } else {
                    toastr.error(response.message || 'Giriş başarısız', 'Hata');
                }
            },
            error: function(xhr) {
                toastr.error(xhr.responseJSON?.message || 'Giriş başarısız', 'Hata');
            }
        });
    });

    // Handle register form submission
    $('#registerForm').submit(function(e) {
        e.preventDefault();
        
        const registerData = {
            firstName: $('#firstName').val(),
            lastName: $('#lastName').val(),
            email: $('#email').val(),
            username: $('#registerUsername').val(),
            password: $('#registerPassword').val()
        };

        $.ajax({
            url: API_SETTINGS.BASE_URL + API_SETTINGS.ENDPOINTS.AUTH.REGISTER,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(registerData),
            success: function(response) {
                if (response.isSuccess) {
                    toastr.success('Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz.', 'Başarılı', {
                        timeOut: 2000,
                        onHidden: function() {
                            window.location.href = '/Home/Login';
                        }
                    });
                } else {
                    toastr.error(response.message || 'Kayıt başarısız', 'Hata');
                }
            },
            error: function(xhr) {
                toastr.error(xhr.responseJSON?.message || 'Kayıt başarısız', 'Hata');
            }
        });
    });

    // Function to update navigation based on auth state
    function updateNavigation() {
        const token = localStorage.getItem('token');
        const user = JSON.parse(localStorage.getItem('user') || 'null');

        if (token && user) {
            // Kullanıcı giriş yapmışsa tüm menüyü göster
            $('.navbar-nav.flex-grow-1').html(`
                
            `);

            $('#userNav').html(`
                <li class="nav-item">
                    <span class="user-welcome">
                        <i class="fas fa-user me-1"></i>Hoş geldiniz, ${user.firstName}
                    </span>
                </li>
                <li class="nav-item">
                    <a class="nav-link" href="#" id="logoutBtn">
                        <i class="fas fa-sign-out-alt me-1"></i>Çıkış Yap
                    </a>
                </li>
            `);

            $('#logoutBtn').click(function() {
                localStorage.removeItem('token');
                localStorage.removeItem('user');
                updateNavigation();
                toastr.success('Başarıyla çıkış yapıldı!', 'Başarılı', {
                    timeOut: 2000,
                    onHidden: function() {
                        window.location.href = '/';
                    }
                });
            });
        } else {
            // Kullanıcı giriş yapmamışsa sadece giriş/kayıt seçeneklerini göster
            $('.navbar-nav.flex-grow-1').html('');
            
            $('#userNav').html(`
                <li class="nav-item">
                    <a class="nav-link" href="/Home/Login">
                        <i class="fas fa-sign-in-alt me-1"></i>Giriş Yap
                    </a>
                </li>
                <li class="nav-item">
                    <a class="nav-link" href="/Home/Register">
                        <i class="fas fa-user-plus me-1"></i>Kayıt Ol
                    </a>
                </li>
            `);
        }
    }

    // Check auth state on page load
    updateNavigation();
}); 