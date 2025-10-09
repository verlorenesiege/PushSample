/**
 * Service Workerの'push'イベントリスナー
 * サーバーからプッシュメッセージが届くと、このイベントが発火します。
 */
self.addEventListener('push', (event) => {
    console.log(' プッシュイベントを受信しました。');

    let data;
    try {
        // プッシュイベントのペイロードをJSONとして解析
        data = event.data.json();
    } catch (e) {
        console.error('プッシュデータの解析に失敗しました:', e);
        // デフォルトのデータを設定
        data = {
            title: '新しい通知',
            body: 'サーバーからメッセージが届きました。',
        };
    }

    const title = data.title || '通知';
    const options = {
        body: data.body || '新しいメッセージがあります。',
        icon: data.icon || '/images/icon-192x192.png', // 通知に表示されるアイコン
        badge: data.badge || '/images/badge-72x72.png', // モバイルのステータスバーなどに表示される小さなアイコン
        tag: 'push-notification-tag', // 同じタグを持つ通知は上書きされ、スパムを防ぐ
        data: {
            url: data.url || '/' // 通知クリック時に開くURL
        }
        // その他のオプションは下記の表を参照
    };

    // showNotification()はPromiseを返すため、event.waitUntil()でラップする
    // これにより、Service Workerが通知を表示し終わるまで終了しないことが保証される
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

/**
 * Service Workerの'notificationclick'イベントリスナー
 * ユーザーが通知をクリックしたときに発火します。
 */
self.addEventListener('notificationclick', (event) => {
    console.log(' 通知がクリックされました。');

    // 通知を閉じる
    event.notification.close();

    const urlToOpen = event.notification.data.url;

    // event.waitUntil()で、非同期処理が完了するまでService Workerを維持する
    event.waitUntil(
        // 現在開いているクライアント（ウィンドウやタブ）を検索
        clients.matchAll({
            type: 'window',
            includeUncontrolled: true
        }).then((clientList) => {
            // 既に同じURLのタブが開いているかチェック
            for (const client of clientList) {
                // `new URL(client.url).pathname` を使用して、クエリパラメータを無視して比較することも可能
                if (client.url === urlToOpen && 'focus' in client) {
                    return client.focus(); // 既存のタブにフォーカスを当てる
                }
            }
            // 該当するタブがなければ、新しいウィンドウでURLを開く
            if (clients.openWindow) {
                return clients.openWindow(urlToOpen);
            }
        })
    );
});
