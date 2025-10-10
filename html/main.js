// VAPIDの公開鍵。サーバーサイドで生成されたものをここに設定します。
// この鍵は、アプリケーションサーバーが正規のものであることをプッシュサービスに証明するために使用されます。
//const VAPID_PRIVATE_KYE = 'dgDATCxnfyLjh7SkCg_bSeuUbaufS1PDMHYOyW1qGBE';
//const VAPID_PUBLIC_KEY = 'BMD8lbQt2rlu-2vmeO2RPOGUAsH6K49mO3QLDIObmi0HE2ZEmPMAkVVZktOIhepK6dfeba8Nm88PgkB4MAyI0g0'; // 必ずサーバーで生成した公開鍵に置き換えてください

const VAPID_PUBLIC_KEY = 'BCrMZpWrJhviBTe76eDmqd9kOGxnHZeIS-iPNGBvd6KjhcLlN6jIprlXLJ519j3B3QybhoNxx3d_AzC-zKiigec';
const VAPID_PRIVATE_KYE = 'Ve_SCxfZxHNI5ElUXP4suC30mBqM9PizvAWxdWGMcSI';


/**
 * URL-safe Base64文字列をUint8Arrayに変換するヘルパー関数
 * @param {string} base64String
 * @returns {Uint8Array}
 */
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
       .replace(/-/g, '+')
       .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

/**
 * メインの実行関数
 */
async function main() {
    const subscribeButton = document.getElementById('subscribeButton');

    // 1. ブラウザがService WorkerとPush APIをサポートしているかチェック
    if (!('serviceWorker' in navigator) ||!('PushManager' in window)) {
        console.warn('Push通知はこのブラウザではサポートされていません。');
        subscribeButton.disabled = true;
        return;
    }

    try {
        // 2. Service Workerを登録
        const registration = await navigator.serviceWorker.register('/sw.js');
        console.log('Service Workerの登録に成功しました。スコープ:', registration.scope);

        // 3. ユーザーの購読状態をチェックし、UIを更新
        const existingSubscription = await registration.pushManager.getSubscription();
        if (existingSubscription) {
            console.log('既に購読済みです。');
            subscribeButton.textContent = '購読解除する'; // 簡単な例として。解除ロジックは別途実装が必要。
            //subscribeButton.disabled = true; // このサンプルでは解除は実装しない
        }

        // 4. 購読ボタンのクリックイベントを設定
        subscribeButton.addEventListener('click', async () => {
            try {
                // 5. 通知の許可をユーザーに要求
                // ユーザーの操作（クリック）に応じて許可を求めるのがベストプラクティス
                const permission = await Notification.requestPermission();
                if (permission!== 'granted') {
                    console.error('通知の許可が得られませんでした。');
                    return;
                }
                console.log('通知の許可が得られました。');

                // 6. プッシュ通知の購読を開始
                const subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true, // すべてのプッシュ通知がユーザーに見えることを保証
                    applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY) // VAPID公開鍵
                });
                console.log('プッシュ通知の購読に成功しました:', subscription);

                // 7. 購読情報をサーバーに送信して保存
                await sendSubscriptionToServer(subscription);
                console.log('購読情報をサーバーに送信しました。');

                subscribeButton.textContent = '購読済み';
                //subscribeButton.disabled = true;

            } catch (error) {
                console.error(error);
                console.error('プッシュ通知の購読に失敗しました:', error);
            }
        });

    } catch (error) {
        console.error('Service Workerの登録に失敗しました:', error);
    }
}

/**
 * 購読情報をサーバーに送信する関数
 * @param {PushSubscription} subscription
 */
async function sendSubscriptionToServer(subscription) {
    const response = await fetch('https://localhost:7270/api/save-subscription', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(subscription),
    });
    console.log(response)
    console.log(response.body.text())
    if (!response.ok) {
        throw new Error('サーバーへの購読情報の送信に失敗しました。');
    }
}

main();
