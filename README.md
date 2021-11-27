ASP.NET Core標準のcookie認証+Google OAuthとかをSPA+WebAPIで使えないかなーという検証。
結論的から言うと出来た。

もともと`Services.AddAuthentication.AddGoogle()`とかこのあたり、基本的に[cookie認証でしか使う事を考慮しておらず](https://github.com/dotnet/aspnetcore/issues/21719#issuecomment-627603872]、JWTとかそんなものは知ったことではないらしい。まぁ、MVCとかのために作ってると思えばそれはそうか感。
(そもそも、JWT認証に関してはデフォルトでビルトインされてなかったりする。なぜなのか。)

SPA + WebAPIの場合、①認証情報をcookieに持たせるか、②jwtをweb storageとかに保存するか、という問題がある。
後者はなんやかんや危険である。
なのでcookieに持たせよう、ということにするなら、WebAPIでも既存の`.AddGoogle()`とかの認証の仕組みに乗っかれるんとちゃうん？という。
別にフロント側からすれば認証時に`Set-Cookie`がレスポンスヘッダにのって渡されるだけといえばそれだけだし、Web API叩く時も毎回それをくっつけて送信すればいいだけだしね。

サーバ側でCORSの設定をして、
```cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("https://localhost:7081")
            .AllowCredentials();
    });
});
```

次に認証の設定をする。
デフォルトでCoockie認証では`[Authorize]`がアノテーションされたアクションに認証されてないリクエストが飛んでくると、自動的にチャレンジさせるためにリダイレクトさせてしまう(何故ならMVCが主なユースケースなので)。
その挙動を`options.Events.OnRedirectToLogin`で潰す。
```cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.LoginPath = string.Empty;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        var googleAuthSection = configuration.GetSection("Authentication:Google");

        options.ClientId = googleAuthSection["ClientId"];
        options.ClientSecret = googleAuthSection["ClientSecret"];
    });
```

クライアント側は`credentials: 'include'`を設定して`fetch`すればよろしい。

```js
fetch("https://localhost:5001/WeatherForecast/Get2", {
    credentials: 'include'
})
```
