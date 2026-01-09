window.checkPermission = async () => {
    if (!('Notification' in window)) {
        console.error('❌ [JS] Notificações não são suportadas pelo navegador');
        return false;
    }
    const permission = Notification.permission;
    return permission === 'granted';
}

window.requestNotificationPermission = async () => {
    if (!('Notification' in window)) {
        return "unsupported";
    }

    const currentPermission = Notification.permission;
    if (currentPermission === 'granted') {
        return 'granted';
    }

    try {
        const permission = await Notification.requestPermission();
        return permission;
    } catch (error) {
        return "error";
    }
}

window.showNotification = (title, body) => {

    try {
        if (!('Notification' in window)) {
            return;
        }

        const currentPermission = Notification.permission;

        if (currentPermission !== 'granted') {
            return;
        }

        const notification = new Notification(title, {
            body: body,
            tag: 'clone-notion-notification'
        });

        notification.onclick = () => {
            notification.close();
            if (window.focus) {
                window.focus();
            }
        };
        
        setTimeout(() => {
            notification.close();
        }, 5000);

    } catch (error) {
        console.error('❌ [JS] Erro ao mostrar notificação:', error);
    }
}