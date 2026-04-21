#!/usr/bin/env python3
"""Add missing translation keys to all Resources.SharedResources.*.resx files.

Strategy:
- Master dict `TRANSLATIONS` maps key -> {lang_code: translated_value}
- For each .resx file, parse existing keys and insert missing ones before </root>
- Preserve existing content (no rewrite)
"""
import os
import re
from pathlib import Path

RESOURCES_DIR = Path("/app/src/AAS.Web/Resources")
LANGS = ["en", "cs", "de", "es", "fr", "hi", "ja", "pt", "ru", "zh"]
# file name pattern: Resources.SharedResources.<lang>.resx, EN is Resources.SharedResources.resx
def file_for(lang):
    if lang == "en":
        return RESOURCES_DIR / "Resources.SharedResources.resx"
    return RESOURCES_DIR / f"Resources.SharedResources.{lang}.resx"

# Master translations - each key has translations in all 10 languages.
# Order of columns: en, cs, de, es, fr, hi, ja, pt, ru, zh
TRANSLATIONS = {
    # --- Navigation / general ---
    "Navigation":                       ["Navigation","Navigace","Navigation","Navegación","Navigation","नेविगेशन","ナビゲーション","Navegação","Навигация","导航"],
    "About us":                         ["About us","O nás","Über uns","Sobre nosotros","À propos","हमारे बारे में","私たちについて","Sobre nós","О нас","关于我们"],
    "Browse":                           ["Browse","Procházet","Durchsuchen","Explorar","Parcourir","ब्राउज़ करें","閲覧","Navegar","Обзор","浏览"],
    "Browse Collections":               ["Browse Collections","Procházet kolekce","Kollektionen durchsuchen","Explorar colecciones","Parcourir les collections","संग्रह ब्राउज़ करें","コレクションを見る","Explorar coleções","Просмотреть коллекции","浏览藏品"],
    "Browse our curated selection of luxury artwork and collectibles": [
        "Browse our curated selection of luxury artwork and collectibles",
        "Projděte si naši pečlivě vybranou kolekci luxusních uměleckých děl a sběratelských předmětů",
        "Entdecken Sie unsere kuratierte Auswahl an Luxuskunst und Sammlerstücken",
        "Explore nuestra selección curada de obras de arte de lujo y coleccionables",
        "Parcourez notre sélection soignée d'œuvres d'art de luxe et d'objets de collection",
        "हमारे चुनिंदा लक्ज़री कलाकृति और संग्रहणीय वस्तुओं का चयन ब्राउज़ करें",
        "厳選された高級アート作品とコレクション品をご覧ください",
        "Explore nossa seleção curada de obras de arte de luxo e colecionáveis",
        "Ознакомьтесь с нашей тщательно подобранной коллекцией предметов роскоши и искусства",
        "浏览我们精心挑选的奢华艺术品和收藏品"
    ],
    "ViewAllCollections":               ["View All Collections","Zobrazit všechny kolekce","Alle Kollektionen ansehen","Ver todas las colecciones","Voir toutes les collections","सभी संग्रह देखें","すべてのコレクションを見る","Ver todas as coleções","Все коллекции","查看所有藏品"],
    "Discover":                         ["Discover","Objevit","Entdecken","Descubrir","Découvrir","खोजें","発見する","Descubrir","Открыть","发现"],
    "Discover Our Collections":         ["Discover Our Collections","Objevte naše kolekce","Entdecken Sie unsere Kollektionen","Descubra nuestras colecciones","Découvrez nos collections","हमारे संग्रह खोजें","私たちのコレクションを発見","Descubra nossas coleções","Откройте наши коллекции","探索我们的藏品"],
    "Featured":                         ["Featured","Doporučené","Empfohlen","Destacados","En vedette","विशेष रुप से प्रदर्शित","注目","Destaque","Избранное","精选"],
    "Latest Additions":                 ["Latest Additions","Novinky","Neueste Ergänzungen","Últimas incorporaciones","Derniers ajouts","नवीनतम जोड़","最新の追加","Adições recentes","Последние поступления","最新添加"],
    "Explore our newest acquisitions from each category": [
        "Explore our newest acquisitions from each category",
        "Prozkoumejte naše nejnovější akvizice z každé kategorie",
        "Entdecken Sie unsere neuesten Erwerbungen aus jeder Kategorie",
        "Explore nuestras adquisiciones más recientes de cada categoría",
        "Explorez nos dernières acquisitions de chaque catégorie",
        "प्रत्येक श्रेणी से हमारे नवीनतम अधिग्रहणों का अन्वेषण करें",
        "各カテゴリーの最新の取得品をご覧ください",
        "Explore nossas aquisições mais recentes de cada categoria",
        "Ознакомьтесь с нашими новейшими поступлениями из каждой категории",
        "探索我们每个类别中的最新收藏"
    ],
    "View All":                         ["View All","Zobrazit vše","Alle ansehen","Ver todo","Voir tout","सभी देखें","すべて表示","Ver tudo","Все","查看全部"],
    "Explore More":                     ["Explore More","Prozkoumat více","Mehr entdecken","Explorar más","En savoir plus","और अन्वेषण करें","もっと見る","Explorar mais","Узнать больше","探索更多"],
    "Verified":                         ["Verified","Ověřeno","Verifiziert","Verificado","Vérifié","सत्यापित","認証済み","Verificado","Проверено","已验证"],

    # --- Contact page ---
    "Reach Out":                        ["Reach Out","Kontaktujte nás","Kontakt","Contáctenos","Contactez-nous","संपर्क करें","お問い合わせ","Entre em contato","Связаться","联系我们"],
    "Reach Out to Us":                  ["Reach Out to Us","Kontaktujte nás","Kontaktieren Sie uns","Contáctenos","Contactez-nous","हमसे संपर्क करें","私たちにご連絡ください","Entre em contato conosco","Свяжитесь с нами","与我们联系"],
    "Reach Out to Us Text":             [
        "Our specialists are ready to answer your questions and provide expert guidance. We guarantee a response within 24 hours.",
        "Naši specialisté jsou připraveni odpovědět na vaše otázky a poskytnout odborné vedení. Garantujeme odpověď do 24 hodin.",
        "Unsere Spezialisten beantworten gerne Ihre Fragen und bieten fachkundige Beratung. Wir garantieren eine Antwort innerhalb von 24 Stunden.",
        "Nuestros especialistas están listos para responder a sus preguntas y brindar orientación experta. Garantizamos una respuesta dentro de 24 horas.",
        "Nos spécialistes sont prêts à répondre à vos questions et à vous offrir des conseils d'experts. Nous garantissons une réponse sous 24 heures.",
        "हमारे विशेषज्ञ आपके प्रश्नों का उत्तर देने और विशेषज्ञ मार्गदर्शन प्रदान करने के लिए तैयार हैं। हम 24 घंटे के भीतर प्रतिक्रिया की गारंटी देते हैं।",
        "弊社の専門家がお客様のご質問にお答えし、専門的なアドバイスをご提供いたします。24時間以内の返信を保証いたします。",
        "Nossos especialistas estão prontos para responder às suas perguntas e fornecer orientação especializada. Garantimos uma resposta em até 24 horas.",
        "Наши специалисты готовы ответить на ваши вопросы и предоставить экспертное руководство. Мы гарантируем ответ в течение 24 часа.",
        "我们的专家随时准备回答您的问题并提供专业指导。我们保证在24小时内回复。"
    ],
    "Send Us a Message":                ["Send Us a Message","Napište nám","Senden Sie uns eine Nachricht","Envíanos un mensaje","Envoyez-nous un message","हमें संदेश भेजें","メッセージを送る","Envie-nos uma mensagem","Отправьте нам сообщение","给我们发送消息"],
    "Send Message":                     ["Send Message","Odeslat zprávu","Nachricht senden","Enviar mensaje","Envoyer le message","संदेश भेजें","メッセージを送信","Enviar mensagem","Отправить сообщение","发送消息"],
    "Your Name":                        ["Your Name","Vaše jméno","Ihr Name","Su nombre","Votre nom","आपका नाम","お名前","Seu nome","Ваше имя","您的姓名"],
    "Enter your name":                  ["Enter your name","Zadejte své jméno","Geben Sie Ihren Namen ein","Ingrese su nombre","Entrez votre nom","अपना नाम दर्ज करें","お名前を入力","Digite seu nome","Введите ваше имя","输入您的姓名"],
    "Enter your email":                 ["Enter your email","Zadejte svůj e-mail","Geben Sie Ihre E-Mail-Adresse ein","Ingrese su correo electrónico","Entrez votre email","अपना ईमेल दर्ज करें","メールアドレスを入力","Digite seu email","Введите ваш email","输入您的电子邮件"],
    "Subject":                          ["Subject","Předmět","Betreff","Asunto","Sujet","विषय","件名","Assunto","Тема","主题"],
    "What is this about?":              ["What is this about?","O čem to je?","Worum geht es?","¿De qué se trata?","De quoi s'agit-il ?","यह किस बारे में है?","内容について","Sobre o que é?","О чем это?","这是关于什么的？"],
    "Message":                          ["Message","Zpráva","Nachricht","Mensaje","Message","संदेश","メッセージ","Mensagem","Сообщение","消息"],
    "Tell us about your inquiry...":    ["Tell us about your inquiry...","Napište nám o vaší žádosti...","Erzählen Sie uns von Ihrer Anfrage...","Cuéntenos sobre su consulta...","Parlez-nous de votre demande...","हमें अपनी पूछताछ के बारे में बताएं...","お問い合わせ内容をお知らせください...","Conte-nos sobre sua consulta...","Расскажите нам о вашем запросе...","告诉我们您的询问..."],
    "Tell us about your interest...":   ["Tell us about your interest...","Napište nám o vašem zájmu...","Erzählen Sie uns von Ihrem Interesse...","Cuéntenos sobre su interés...","Parlez-nous de votre intérêt...","हमें अपनी रुचि के बारे में बताएं...","ご興味についてお知らせください...","Conte-nos sobre seu interesse...","Расскажите о своем интересе...","告诉我们您的兴趣..."],
    "Get In Touch":                     ["Get In Touch","Spojte se s námi","Kontakt aufnehmen","Póngase en contacto","Contactez-nous","संपर्क में रहें","お問い合わせ","Entre em contato","Свяжитесь с нами","联系我们"],
    "Get in Touch":                     ["Get in Touch","Spojte se s námi","Kontakt aufnehmen","Póngase en contacto","Contactez-nous","संपर्क में रहें","お問い合わせ","Entre em contato","Свяжитесь с нами","联系我们"],
    "Get in touch with our team":       ["Get in touch with our team","Spojte se s naším týmem","Nehmen Sie Kontakt mit unserem Team auf","Póngase en contacto con nuestro equipo","Contactez notre équipe","हमारी टीम से संपर्क करें","私たちのチームにお問い合わせください","Entre em contato com nossa equipe","Свяжитесь с нашей командой","联系我们的团队"],
    "Follow Us":                        ["Follow Us","Sledujte nás","Folgen Sie uns","Síganos","Suivez-nous","हमें फॉलो करें","フォローする","Siga-nos","Следите за нами","关注我们"],
    "Email Address":                    ["Email Address","E-mailová adresa","E-Mail-Adresse","Dirección de correo","Adresse e-mail","ईमेल पता","メールアドレス","Endereço de email","Адрес электронной почты","电子邮件地址"],
    "Location":                         ["Location","Místo","Standort","Ubicación","Emplacement","स्थान","所在地","Localização","Местоположение","位置"],
    "Houston, Texas, USA":              ["Houston, Texas, USA","Houston, Texas, USA","Houston, Texas, USA","Houston, Texas, EE.UU.","Houston, Texas, États-Unis","ह्यूस्टन, टेक्सास, यूएसए","ヒューストン、テキサス、米国","Houston, Texas, EUA","Хьюстон, Техас, США","美国德克萨斯州休斯顿"],
    "Worldwide representation available":["Worldwide representation available","K dispozici celosvětové zastoupení","Weltweite Vertretung verfügbar","Representación mundial disponible","Représentation mondiale disponible","विश्वव्यापी प्रतिनिधित्व उपलब्ध","世界中での代理対応可能","Representação mundial disponível","Доступно представительство по всему миру","提供全球代理"],
    "Response Time":                    ["Response Time","Doba odezvy","Antwortzeit","Tiempo de respuesta","Temps de réponse","प्रतिक्रिया समय","応答時間","Tempo de resposta","Время ответа","响应时间"],
    "Within 24 hours":                  ["Within 24 hours","Do 24 hodin","Innerhalb von 24 Stunden","Dentro de 24 horas","Sous 24 heures","24 घंटे के भीतर","24時間以内","Em até 24 horas","В течение 24 часа","24小时内"],
    "Professional and confidential service":["Professional and confidential service","Profesionální a důvěrný servis","Professioneller und vertraulicher Service","Servicio profesional y confidencial","Service professionnel et confidentiel","पेशेवर और गोपनीय सेवा","プロフェッショナルで機密性の高いサービス","Serviço profissional e confidencial","Профессиональный и конфиденциальный сервис","专业且保密的服务"],
    "Privacy Notice":                   ["Privacy Notice","Upozornění o soukromí","Datenschutzhinweis","Aviso de privacidad","Avis de confidentialité","गोपनीयता सूचना","プライバシー通知","Aviso de privacidade","Уведомление о конфиденциальности","隐私声明"],
    "Privacy Notice Text":              [
        "All communication is conducted with the utmost confidentiality. Your personal information and inquiries are kept secure.",
        "Veškerá komunikace probíhá s maximální důvěrností. Vaše osobní údaje a dotazy jsou v bezpečí.",
        "Die gesamte Kommunikation erfolgt mit höchster Vertraulichkeit. Ihre persönlichen Daten und Anfragen werden sicher aufbewahrt.",
        "Toda comunicación se realiza con la máxima confidencialidad. Su información personal y consultas se mantienen seguras.",
        "Toute communication est menée avec la plus grande confidentialité. Vos informations personnelles et demandes restent sécurisées.",
        "सभी संचार अत्यंत गोपनीयता के साथ किया जाता है। आपकी व्यक्तिगत जानकारी और पूछताछ सुरक्षित रखी जाती है।",
        "すべてのコミュニケーションは最高の機密性をもって行われます。お客様の個人情報とお問い合わせは安全に保管されます。",
        "Toda a comunicação é conduzida com a máxima confidencialidade. Suas informações pessoais e consultas são mantidas seguras.",
        "Вся коммуникация осуществляется с максимальной конфиденциальностью. Ваша личная информация и запросы находятся в безопасности.",
        "所有通信都以最高的保密性进行。您的个人信息和查询将得到妥善保护。"
    ],
    "Ready to Begin?":                  ["Ready to Begin?","Připraveni začít?","Bereit anzufangen?","¿Listo para empezar?","Prêt à commencer ?","शुरू करने के लिए तैयार?","始める準備はできましたか？","Pronto para começar?","Готовы начать?","准备开始？"],
    "Ready to Start?":                  ["Ready to Start?","Připraveni začít?","Bereit loszulegen?","¿Listo para comenzar?","Prêt à démarrer ?","शुरू करने के लिए तैयार?","開始の準備はできましたか？","Pronto para começar?","Готовы начать?","准备开始？"],
    "Start Your Journey With Us":       ["Start Your Journey With Us","Začněte svou cestu s námi","Beginnen Sie Ihre Reise mit uns","Comience su viaje con nosotros","Commencez votre voyage avec nous","हमारे साथ अपनी यात्रा शुरू करें","私たちと一緒に旅を始めましょう","Comece sua jornada conosco","Начните свой путь с нами","与我们开启您的旅程"],
    "Begin Your Journey Today":         ["Begin Your Journey Today","Začněte svou cestu ještě dnes","Beginnen Sie Ihre Reise heute","Comience su viaje hoy","Commencez votre voyage aujourd'hui","आज ही अपनी यात्रा शुरू करें","今日から旅を始めよう","Comece sua jornada hoje","Начните свой путь сегодня","今天就开启您的旅程"],

    # --- Blog / comments ---
    "Featured Image":                   ["Featured Image","Hlavní obrázek","Beitragsbild","Imagen destacada","Image à la une","विशेष रुप से प्रदर्शित छवि","注目の画像","Imagem em destaque","Главное изображение","特色图片"],
    "Current Featured Image":           ["Current Featured Image","Aktuální hlavní obrázek","Aktuelles Beitragsbild","Imagen destacada actual","Image à la une actuelle","वर्तमान विशेष छवि","現在の注目画像","Imagem em destaque atual","Текущее главное изображение","当前特色图片"],
    "Optional: Leave empty to keep current image":[
        "Optional: Leave empty to keep current image","Volitelné: Ponechejte prázdné pro zachování aktuálního obrázku",
        "Optional: Leer lassen, um das aktuelle Bild zu behalten","Opcional: Deje vacío para mantener la imagen actual",
        "Optionnel : Laissez vide pour conserver l'image actuelle","वैकल्पिक: वर्तमान छवि रखने के लिए खाली छोड़ें",
        "オプション：現在の画像を保持するには空のままにしてください","Opcional: Deixe em branco para manter a imagem atual",
        "Необязательно: Оставьте пустым, чтобы сохранить текущее изображение","可选：留空以保留当前图片"
    ],
    "Optional: Main image for the blog post":[
        "Optional: Main image for the blog post","Volitelné: Hlavní obrázek pro blogový příspěvek",
        "Optional: Hauptbild für den Blogbeitrag","Opcional: Imagen principal del artículo",
        "Optionnel : Image principale de l'article","वैकल्पिक: ब्लॉग पोस्ट के लिए मुख्य छवि",
        "オプション：ブログ投稿のメイン画像","Opcional: Imagem principal do artigo",
        "Необязательно: Главное изображение для записи блога","可选：博客文章的主图片"
    ],
    "Blog Title":                       ["Blog Title","Název článku","Blog-Titel","Título del blog","Titre du blog","ब्लॉग शीर्षक","ブログタイトル","Título do blog","Заголовок блога","博客标题"],
    "Blog Content":                     ["Blog Content","Obsah článku","Blog-Inhalt","Contenido del blog","Contenu du blog","ब्लॉग सामग्री","ブログ内容","Conteúdo do blog","Содержание блога","博客内容"],
    "Create Blog Post":                 ["Create Blog Post","Vytvořit článek","Blogbeitrag erstellen","Crear publicación","Créer un article","ब्लॉग पोस्ट बनाएं","ブログ投稿を作成","Criar publicação","Создать запись блога","创建博客文章"],
    "Edit Blog Post":                   ["Edit Blog Post","Upravit článek","Blogbeitrag bearbeiten","Editar publicación","Modifier l'article","ब्लॉग पोस्ट संपादित करें","ブログ投稿を編集","Editar publicação","Редактировать запись блога","编辑博客文章"],
    "Delete Blog Post":                 ["Delete Blog Post","Smazat článek","Blogbeitrag löschen","Eliminar publicación","Supprimer l'article","ब्लॉग पोस्ट हटाएं","ブログ投稿を削除","Excluir publicação","Удалить запись блога","删除博客文章"],
    "Are you sure you want to delete this blog post?":[
        "Are you sure you want to delete this blog post?","Opravdu chcete smazat tento článek?",
        "Möchten Sie diesen Blogbeitrag wirklich löschen?","¿Está seguro de que desea eliminar esta publicación?",
        "Êtes-vous sûr de vouloir supprimer cet article ?","क्या आप वाकई इस ब्लॉग पोस्ट को हटाना चाहते हैं?",
        "このブログ投稿を本当に削除しますか？","Tem certeza de que deseja excluir esta publicação?",
        "Вы уверены, что хотите удалить эту запись блога?","您确定要删除此博客文章吗？"
    ],
    "Are you sure?":                    ["Are you sure?","Jste si jistí?","Sind Sie sicher?","¿Está seguro?","Êtes-vous sûr ?","क्या आप निश्चित हैं?","よろしいですか？","Tem certeza?","Вы уверены?","您确定吗？"],
    "Confirm Deletion":                 ["Confirm Deletion","Potvrdit smazání","Löschen bestätigen","Confirmar eliminación","Confirmer la suppression","हटाने की पुष्टि करें","削除を確認","Confirmar exclusão","Подтвердите удаление","确认删除"],
    "Draft":                            ["Draft","Koncept","Entwurf","Borrador","Brouillon","ड्राफ़्ट","下書き","Rascunho","Черновик","草稿"],
    "Published":                        ["Published","Publikováno","Veröffentlicht","Publicado","Publié","प्रकाशित","公開済み","Publicado","Опубликовано","已发布"],
    "Read More":                        ["Read More","Číst více","Mehr lesen","Leer más","Lire la suite","और पढ़ें","続きを読む","Leia mais","Читать далее","阅读更多"],
    "New Post":                         ["New Post","Nový příspěvek","Neuer Beitrag","Nueva publicación","Nouvel article","नया पोस्ट","新しい投稿","Nova publicação","Новая запись","新文章"],
    "No blog posts available":          ["No blog posts available","Žádné příspěvky nejsou k dispozici","Keine Blogbeiträge verfügbar","No hay publicaciones disponibles","Aucun article disponible","कोई ब्लॉग पोस्ट उपलब्ध नहीं","ブログ投稿はありません","Nenhum artigo disponível","Нет доступных записей блога","暂无博客文章"],
    "Check back soon for news and insights":[
        "Check back soon for news and insights","Brzy se vraťte pro novinky a postřehy",
        "Schauen Sie bald wieder vorbei für Neuigkeiten","Vuelva pronto para noticias e ideas",
        "Revenez bientôt pour des actualités et des analyses","समाचार और अंतर्दृष्टि के लिए जल्द ही वापस देखें",
        "ニュースや洞察のためにすぐに戻ってきてください","Volte em breve para notícias e insights",
        "Скоро возвращайтесь за новостями","请稍后返回查看新闻和见解"
    ],
    "Comments":                         ["Comments","Komentáře","Kommentare","Comentarios","Commentaires","टिप्पणियाँ","コメント","Comentários","Комментарии","评论"],
    "Add Comment":                      ["Add Comment","Přidat komentář","Kommentar hinzufügen","Agregar comentario","Ajouter un commentaire","टिप्पणी जोड़ें","コメントを追加","Adicionar comentário","Добавить комментарий","添加评论"],
    "Submit Comment":                   ["Submit Comment","Odeslat komentář","Kommentar senden","Enviar comentario","Envoyer le commentaire","टिप्पणी सबमिट करें","コメントを投稿","Enviar comentário","Отправить комментарий","提交评论"],
    "Edit Comment":                     ["Edit Comment","Upravit komentář","Kommentar bearbeiten","Editar comentario","Modifier le commentaire","टिप्पणी संपादित करें","コメントを編集","Editar comentário","Редактировать комментарий","编辑评论"],
    "Delete Comment":                   ["Delete Comment","Smazat komentář","Kommentar löschen","Eliminar comentario","Supprimer le commentaire","टिप्पणी हटाएं","コメントを削除","Excluir comentário","Удалить комментарий","删除评论"],
    "Your Comment":                     ["Your Comment","Váš komentář","Ihr Kommentar","Su comentario","Votre commentaire","आपकी टिप्पणी","あなたのコメント","Seu comentário","Ваш комментарий","您的评论"],
    "Write your comment...":            ["Write your comment...","Napište svůj komentář...","Schreiben Sie Ihren Kommentar...","Escriba su comentario...","Écrivez votre commentaire...","अपनी टिप्पणी लिखें...","コメントを書いてください...","Escreva seu comentário...","Напишите ваш комментарий...","写下您的评论..."],
    "Write your comment here...":       ["Write your comment here...","Napište svůj komentář zde...","Schreiben Sie hier Ihren Kommentar...","Escriba su comentario aquí...","Écrivez votre commentaire ici...","अपनी टिप्पणी यहाँ लिखें...","ここにコメントを書いてください...","Escreva seu comentário aqui...","Напишите комментарий здесь...","在此处写下您的评论..."],
    "Edit your comment:":               ["Edit your comment:","Upravte svůj komentář:","Bearbeiten Sie Ihren Kommentar:","Edite su comentario:","Modifier votre commentaire :","अपनी टिप्पणी संपादित करें:","コメントを編集してください：","Edite seu comentário:","Редактируйте ваш комментарий:","编辑您的评论："],
    "edited":                           ["edited","upraveno","bearbeitet","editado","modifié","संपादित","編集済み","editado","изменено","已编辑"],
    "No comments yet. Be the first to comment!":[
        "No comments yet. Be the first to comment!","Zatím žádné komentáře. Buďte první!",
        "Noch keine Kommentare. Seien Sie der Erste!","Aún no hay comentarios. ¡Sea el primero en comentar!",
        "Aucun commentaire pour l'instant. Soyez le premier à commenter !","अभी तक कोई टिप्पणी नहीं। पहले टिप्पणी करने वाले बनें!",
        "まだコメントはありません。最初のコメントを投稿してください！","Ainda sem comentários. Seja o primeiro a comentar!",
        "Комментариев пока нет. Оставьте первый!","暂无评论。成为第一个评论的人吧！"
    ],
    "to leave a comment":               ["to leave a comment","pro zanechání komentáře","um einen Kommentar zu hinterlassen","para dejar un comentario","pour laisser un commentaire","टिप्पणी करने के लिए","コメントを残すには","para deixar um comentário","чтобы оставить комментарий","发表评论"],
    "Sign in":                          ["Sign in","Přihlásit se","Anmelden","Iniciar sesión","Se connecter","साइन इन करें","サインイン","Entrar","Войти","登录"],
    "Are you sure you want to delete this comment?":[
        "Are you sure you want to delete this comment?","Opravdu chcete smazat tento komentář?",
        "Möchten Sie diesen Kommentar wirklich löschen?","¿Está seguro de que desea eliminar este comentario?",
        "Êtes-vous sûr de vouloir supprimer ce commentaire ?","क्या आप वाकई इस टिप्पणी को हटाना चाहते हैं?",
        "このコメントを本当に削除しますか？","Tem certeza de que deseja excluir este comentário?",
        "Вы уверены, что хотите удалить этот комментарий?","您确定要删除这条评论吗？"
    ],
    "Do you really want to delete this comment?":[
        "Do you really want to delete this comment?","Opravdu chcete smazat tento komentář?",
        "Möchten Sie diesen Kommentar wirklich löschen?","¿Realmente desea eliminar este comentario?",
        "Voulez-vous vraiment supprimer ce commentaire ?","क्या आप वाकई इस टिप्पणी को हटाना चाहते हैं?",
        "本当にこのコメントを削除しますか？","Você realmente deseja excluir este comentário?",
        "Вы действительно хотите удалить этот комментарий?","您真的要删除这条评论吗？"
    ],
    "Comment cannot be empty":          ["Comment cannot be empty","Komentář nemůže být prázdný","Kommentar darf nicht leer sein","El comentario no puede estar vacío","Le commentaire ne peut pas être vide","टिप्पणी खाली नहीं हो सकती","コメントは空にできません","O comentário não pode estar vazio","Комментарий не может быть пустым","评论不能为空"],
    "Comment is too long (max 2000 characters)":[
        "Comment is too long (max 2000 characters)","Komentář je příliš dlouhý (max 2000 znaků)",
        "Kommentar ist zu lang (max. 2000 Zeichen)","El comentario es demasiado largo (máx. 2000 caracteres)",
        "Le commentaire est trop long (max 2000 caractères)","टिप्पणी बहुत लंबी है (अधिकतम 2000 वर्ण)",
        "コメントが長すぎます（最大2000文字）","O comentário é muito longo (máx. 2000 caracteres)",
        "Комментарий слишком длинный (макс. 2000 символов)","评论过长（最多2000个字符）"
    ],
    "Maximum 2000 characters":          ["Maximum 2000 characters","Maximálně 2000 znaků","Maximal 2000 Zeichen","Máximo 2000 caracteres","Maximum 2000 caractères","अधिकतम 2000 वर्ण","最大2000文字","Máximo 2000 caracteres","Максимум 2000 символов","最多2000个字符"],
    "Failed to load comments":          ["Failed to load comments","Nepodařilo se načíst komentáře","Kommentare konnten nicht geladen werden","Error al cargar comentarios","Impossible de charger les commentaires","टिप्पणियाँ लोड करने में विफल","コメントの読み込みに失敗","Falha ao carregar comentários","Не удалось загрузить комментарии","加载评论失败"],
    "Failed to post comment. Please try again.":[
        "Failed to post comment. Please try again.","Nepodařilo se odeslat komentář. Zkuste to znovu.",
        "Kommentar konnte nicht gesendet werden. Bitte versuchen Sie es erneut.","Error al publicar el comentario. Inténtelo de nuevo.",
        "Échec de la publication du commentaire. Veuillez réessayer.","टिप्पणी पोस्ट करने में विफल। कृपया पुन: प्रयास करें।",
        "コメントの投稿に失敗しました。もう一度お試しください。","Falha ao publicar comentário. Tente novamente.",
        "Не удалось отправить комментарий. Попробуйте снова.","发布评论失败。请重试。"
    ],
    "Failed to update comment. Please try again.":[
        "Failed to update comment. Please try again.","Nepodařilo se aktualizovat komentář. Zkuste to znovu.",
        "Kommentar konnte nicht aktualisiert werden.","Error al actualizar el comentario. Inténtelo de nuevo.",
        "Échec de la mise à jour du commentaire. Veuillez réessayer.","टिप्पणी अपडेट करने में विफल।",
        "コメントの更新に失敗しました。","Falha ao atualizar comentário. Tente novamente.",
        "Не удалось обновить комментарий.","更新评论失败。请重试。"
    ],
    "Failed to delete comment. Please try again.":[
        "Failed to delete comment. Please try again.","Nepodařilo se smazat komentář. Zkuste to znovu.",
        "Kommentar konnte nicht gelöscht werden.","Error al eliminar el comentario. Inténtelo de nuevo.",
        "Échec de la suppression du commentaire. Veuillez réessayer.","टिप्पणी हटाने में विफल।",
        "コメントの削除に失敗しました。","Falha ao excluir comentário. Tente novamente.",
        "Не удалось удалить комментарий.","删除评论失败。请重试。"
    ],
    "Updated":                          ["Updated","Aktualizováno","Aktualisiert","Actualizado","Mis à jour","अद्यतन","更新済み","Atualizado","Обновлено","已更新"],
    "Save Changes":                     ["Save Changes","Uložit změny","Änderungen speichern","Guardar cambios","Enregistrer","परिवर्तन सहेजें","変更を保存","Salvar alterações","Сохранить изменения","保存更改"],
    "Saving...":                        ["Saving...","Ukládání...","Speichert...","Guardando...","Enregistrement...","सहेज रहा है...","保存中...","Salvando...","Сохранение...","保存中..."],
    "Deleting...":                      ["Deleting...","Mazání...","Löscht...","Eliminando...","Suppression...","हटा रहा है...","削除中...","Excluindo...","Удаление...","删除中..."],
    "Uncheck to save as draft":         ["Uncheck to save as draft","Zrušte zaškrtnutí pro uložení jako koncept","Deaktivieren, um als Entwurf zu speichern","Desmarque para guardar como borrador","Décochez pour enregistrer comme brouillon","ड्राफ़्ट के रूप में सहेजने के लिए अनचेक करें","下書きとして保存するにはチェックを外します","Desmarque para salvar como rascunho","Снимите флажок, чтобы сохранить как черновик","取消选中以另存为草稿"],
    "Create":                           ["Create","Vytvořit","Erstellen","Crear","Créer","बनाएं","作成","Criar","Создать","创建"],
    "Changes will be automatically translated using DeepL":[
        "Changes will be automatically translated using DeepL","Změny budou automaticky přeloženy pomocí DeepL",
        "Änderungen werden automatisch mit DeepL übersetzt","Los cambios se traducirán automáticamente con DeepL",
        "Les modifications seront automatiquement traduites avec DeepL","DeepL का उपयोग करके परिवर्तन स्वचालित रूप से अनुवादित होंगे",
        "変更はDeepLで自動翻訳されます","As alterações serão traduzidas automaticamente usando DeepL",
        "Изменения будут автоматически переведены с помощью DeepL","更改将使用DeepL自动翻译"
    ],
    "Content will be automatically translated to all supported languages":[
        "Content will be automatically translated to all supported languages","Obsah bude automaticky přeložen do všech podporovaných jazyků",
        "Inhalt wird automatisch in alle unterstützten Sprachen übersetzt","El contenido se traducirá automáticamente a todos los idiomas",
        "Le contenu sera automatiquement traduit dans toutes les langues","सामग्री सभी समर्थित भाषाओं में स्वचालित रूप से अनुवादित होगी",
        "コンテンツは対応するすべての言語に自動翻訳されます","O conteúdo será traduzido automaticamente para todos os idiomas",
        "Контент будет автоматически переведён на все поддерживаемые языки","内容将自动翻译为所有支持的语言"
    ],
    "Content will be automatically re-translated to all supported languages":[
        "Content will be automatically re-translated to all supported languages","Obsah bude automaticky znovu přeložen do všech podporovaných jazyků",
        "Inhalt wird automatisch in alle unterstützten Sprachen neu übersetzt","El contenido se retraducirá automáticamente a todos los idiomas",
        "Le contenu sera automatiquement retraduit dans toutes les langues","सामग्री सभी समर्थित भाषाओं में स्वचालित रूप से पुनः अनुवादित होगी",
        "コンテンツは対応するすべての言語に自動的に再翻訳されます","O conteúdo será retraduzido automaticamente para todos os idiomas",
        "Контент будет автоматически переведён заново на все поддерживаемые языки","内容将自动重新翻译为所有支持的语言"
    ],
    "Other languages will be translated automatically using DeepL":[
        "Other languages will be translated automatically using DeepL","Ostatní jazyky budou automaticky přeloženy pomocí DeepL",
        "Andere Sprachen werden automatisch mit DeepL übersetzt","Otros idiomas se traducirán automáticamente con DeepL",
        "Les autres langues seront traduites automatiquement avec DeepL","अन्य भाषाएँ DeepL का उपयोग करके स्वचालित रूप से अनुवादित होंगी",
        "他の言語はDeepLで自動翻訳されます","Outros idiomas serão traduzidos automaticamente usando DeepL",
        "Другие языки будут автоматически переведены с помощью DeepL","其他语言将使用DeepL自动翻译"
    ],
    "Content not available in the selected language. Please try switching to Czech.":[
        "Content not available in the selected language. Please try switching to Czech.","Obsah není v vybraném jazyce dostupný. Zkuste přepnout na češtinu.",
        "Inhalt in der gewählten Sprache nicht verfügbar. Bitte wechseln Sie zu Tschechisch.","Contenido no disponible en el idioma seleccionado. Intente cambiar a checo.",
        "Contenu non disponible dans la langue sélectionnée. Veuillez essayer le tchèque.","चयनित भाषा में सामग्री उपलब्ध नहीं है। कृपया चेक पर स्विच करें।",
        "選択された言語でコンテンツが利用できません。チェコ語に切り替えてください。","Conteúdo indisponível no idioma selecionado. Tente mudar para o tcheco.",
        "Содержимое недоступно на выбранном языке. Попробуйте переключиться на чешский.","所选语言中没有内容。请尝试切换到捷克语。"
    ],
    "Link copied to clipboard!":        ["Link copied to clipboard!","Odkaz zkopírován do schránky!","Link in die Zwischenablage kopiert!","¡Enlace copiado al portapapeles!","Lien copié dans le presse-papiers !","लिंक क्लिपबोर्ड पर कॉपी किया गया!","リンクをクリップボードにコピーしました！","Link copiado para a área de transferência!","Ссылка скопирована в буфер обмена!","链接已复制到剪贴板！"],
    "Share this collection":            ["Share this collection","Sdílet tuto kolekci","Diese Kollektion teilen","Compartir esta colección","Partager cette collection","इस संग्रह को साझा करें","このコレクションを共有","Compartilhe esta coleção","Поделиться этой коллекцией","分享此收藏"],

    # --- AAS Verified / How To ---
    "AASVerified":                      ["AAS Verified","Ověřeno AAS","AAS Verifiziert","Verificado por AAS","Vérifié AAS","AAS सत्यापित","AAS認証済み","Verificado AAS","Проверено AAS","AAS 已认证"],
    "AASVerifiedBadge":                 ["AAS Verified","Ověřeno AAS","AAS Verifiziert","Verificado AAS","AAS Vérifié","AAS सत्यापित","AAS認証済み","AAS Verificado","Проверено AAS","AAS 已认证"],
    "AASVerifiedTooltip":               [
        "Authenticity and provenance verified by Aristocratic Artwork Sale experts","Pravost a původ ověřené odborníky Aristocratic Artwork Sale",
        "Echtheit und Herkunft von Aristocratic Artwork Sale-Experten verifiziert","Autenticidad y procedencia verificadas por expertos de AAS",
        "Authenticité et provenance vérifiées par les experts d'AAS","AAS विशेषज्ञों द्वारा प्रामाणिकता और उत्पत्ति सत्यापित",
        "AAS専門家による真正性と来歴の検証済み","Autenticidade e procedência verificadas pelos especialistas da AAS",
        "Подлинность и происхождение проверены экспертами Aristocratic Artwork Sale","由 Aristocratic Artwork Sale 专家验证真实性和来源"
    ],
    "AASVerifiedExplanation":           [
        "AAS Verified items have undergone rigorous authentication by our team of expert curators, guaranteeing authenticity, provenance and quality.",
        "Položky ověřené AAS prošly důkladným ověřením naším týmem expertních kurátorů, což zaručuje pravost, původ a kvalitu.",
        "AAS-verifizierte Artikel wurden von unserem Expertenteam gründlich authentifiziert und garantieren Echtheit, Herkunft und Qualität.",
        "Los artículos verificados por AAS han sido autenticados rigurosamente por nuestro equipo de curadores expertos, garantizando autenticidad, procedencia y calidad.",
        "Les articles AAS vérifiés ont été authentifiés rigoureusement par notre équipe de conservateurs experts, garantissant authenticité, provenance et qualité.",
        "AAS सत्यापित वस्तुओं को हमारे विशेषज्ञ क्यूरेटरों की टीम द्वारा कठोर प्रमाणीकरण से गुज़रा है, जो प्रामाणिकता, उत्पत्ति और गुणवत्ता की गारंटी देता है।",
        "AAS認証済みの商品は、当社の専門キュレーターチームによる厳格な認証を経ており、真正性、来歴、品質が保証されています。",
        "Os itens verificados pela AAS passaram por autenticação rigorosa pela nossa equipe de curadores especialistas, garantindo autenticidade, procedência e qualidade.",
        "Предметы, проверенные AAS, прошли тщательную аутентификацию нашей командой экспертов-кураторов, гарантирующих подлинность, происхождение и качество.",
        "AAS 认证商品经过我们的专家策展人团队严格验证，保证真实性、来源和质量。"
    ],
    "How To":                           ["How To","Jak postupovat","Anleitung","Cómo","Comment","कैसे करें","使い方","Como fazer","Как","如何操作"],
    "How to Buy":                       ["How to Buy","Jak nakupovat","Wie kaufen","Cómo comprar","Comment acheter","कैसे खरीदें","購入方法","Como comprar","Как купить","如何购买"],
    "How to Sell":                      ["How to Sell","Jak prodávat","Wie verkaufen","Cómo vender","Comment vendre","कैसे बेचें","販売方法","Como vender","Как продать","如何出售"],
    "Buying Process":                   ["Buying Process","Proces nákupu","Kaufprozess","Proceso de compra","Processus d'achat","खरीद प्रक्रिया","購入プロセス","Processo de compra","Процесс покупки","购买流程"],
    "Selling Process":                  ["Selling Process","Proces prodeje","Verkaufsprozess","Proceso de venta","Processus de vente","विक्रय प्रक्रिया","販売プロセス","Processo de venda","Процесс продажи","销售流程"],
    "Step":                             ["Step","Krok","Schritt","Paso","Étape","कदम","ステップ","Passo","Шаг","步骤"],
    "How It Works":                     ["How It Works","Jak to funguje","Wie es funktioniert","Cómo funciona","Comment ça marche","यह कैसे काम करता है","仕組み","Como funciona","Как это работает","运作方式"],
    "Guide":                            ["Guide","Průvodce","Anleitung","Guía","Guide","मार्गदर्शिका","ガイド","Guia","Руководство","指南"],

    # --- Categories additional ---
    "Fine Art & Paintings":             ["Fine Art & Paintings","Výtvarné umění a malby","Bildende Kunst & Gemälde","Arte y Pinturas","Beaux-arts et Peintures","ललित कला और पेंटिंग","ファインアートと絵画","Artes Plásticas e Pinturas","Изобразительное искусство и живопись","美术与绘画"],
    "Fine Art & Paintings Desc":        [
        "Curated paintings from renowned artists and emerging talents","Vybrané obrazy od známých umělců a vycházejících talentů",
        "Kuratierte Gemälde von renommierten Künstlern","Pinturas seleccionadas de artistas reconocidos",
        "Peintures sélectionnées d'artistes renommés","प्रसिद्ध कलाकारों से क्यूरेटेड पेंटिंग",
        "著名アーティストと新進気鋭の才能による厳選された絵画","Pinturas curadas de artistas renomados",
        "Отобранные картины известных художников и подающих надежды талантов","来自知名艺术家和新兴人才的精选画作"
    ],
    "Sculptures & Statues":             ["Sculptures & Statues","Sochy a plastiky","Skulpturen & Statuen","Esculturas y Estatuas","Sculptures et Statues","मूर्तियां और प्रतिमाएँ","彫刻と像","Esculturas e Estátuas","Скульптуры и статуи","雕塑与雕像"],
    "Sculptures & Statues Desc":        [
        "Classical and contemporary sculptures for distinguished collectors","Klasické a současné sochy pro náročné sběratele",
        "Klassische und zeitgenössische Skulpturen für anspruchsvolle Sammler","Esculturas clásicas y contemporáneas para coleccionistas distinguidos",
        "Sculptures classiques et contemporaines pour collectionneurs distingués","प्रतिष्ठित संग्राहकों के लिए क्लासिक और समकालीन मूर्तियां",
        "洗練されたコレクターのためのクラシックおよび現代彫刻","Esculturas clássicas e contemporâneas para colecionadores distintos",
        "Классические и современные скульптуры для взыскательных коллекционеров","为尊贵收藏家提供的古典与当代雕塑"
    ],
    "Antiques & Collectibles":          ["Antiques & Collectibles","Starožitnosti a sběratelské předměty","Antiquitäten & Sammlerstücke","Antigüedades y Coleccionables","Antiquités et Objets de collection","प्राचीन वस्तुएं और संग्रहणीय","アンティークとコレクティブル","Antiguidades e Colecionáveis","Антиквариат и коллекционные предметы","古董与收藏品"],
    "Antiques & Collectibles Desc":     [
        "Rare antiques and unique collectible items with rich history","Vzácné starožitnosti a unikátní sběratelské předměty s bohatou historií",
        "Seltene Antiquitäten und einzigartige Sammlerstücke","Antigüedades raras y coleccionables únicos con rica historia",
        "Antiquités rares et objets uniques avec une histoire riche","समृद्ध इतिहास के साथ दुर्लभ प्राचीन वस्तुएं",
        "豊かな歴史を持つ珍しいアンティークとユニークなコレクティブル","Antiguidades raras e itens colecionáveis únicos",
        "Редкий антиквариат и уникальные коллекционные предметы с богатой историей","具有丰富历史的稀有古董和独特收藏品"
    ],
    "Expert valuations and sales of decorative art pieces":[
        "Expert valuations and sales of decorative art pieces","Odborné odhady a prodej dekorativních uměleckých děl",
        "Fachkundige Bewertungen und Verkauf dekorativer Kunstwerke","Tasaciones expertas y venta de piezas de arte decorativo",
        "Évaluations expertes et ventes d'œuvres d'art décoratives","सजावटी कला के विशेषज्ञ मूल्यांकन और बिक्री",
        "装飾美術品の専門的な評価と販売","Avaliações especializadas e vendas de peças de arte decorativa",
        "Экспертные оценки и продажа декоративного искусства","装饰艺术作品的专业评估和销售"
    ],

    # --- Cookie banner (full set) ---
    "Essential Cookies":                ["Essential Cookies","Nezbytné cookies","Notwendige Cookies","Cookies esenciales","Cookies essentiels","आवश्यक कुकीज़","必須クッキー","Cookies essenciais","Необходимые файлы cookie","必要的Cookie"],
    "Analytics Cookies":                ["Analytics Cookies","Analytické cookies","Analyse-Cookies","Cookies analíticas","Cookies d'analyse","विश्लेषण कुकीज़","分析クッキー","Cookies de análise","Аналитические файлы cookie","分析Cookie"],
    "Marketing Cookies":                ["Marketing Cookies","Marketingové cookies","Marketing-Cookies","Cookies de marketing","Cookies marketing","मार्केटिंग कुकीज़","マーケティングクッキー","Cookies de marketing","Маркетинговые файлы cookie","营销Cookie"],
    "Cookie Preferences":               ["Cookie Preferences","Nastavení cookies","Cookie-Einstellungen","Preferencias de cookies","Préférences de cookies","कुकी प्राथमिकताएँ","クッキー設定","Preferências de cookies","Настройки файлов cookie","Cookie 偏好"],
    "Save Preferences":                 ["Save Preferences","Uložit nastavení","Einstellungen speichern","Guardar preferencias","Enregistrer les préférences","प्राथमिकताएँ सहेजें","設定を保存","Salvar preferências","Сохранить предпочтения","保存偏好"],
    "Privacy Policy":                   ["Privacy Policy","Ochrana soukromí","Datenschutzrichtlinie","Política de privacidad","Politique de confidentialité","गोपनीयता नीति","プライバシーポリシー","Política de Privacidade","Политика конфиденциальности","隐私政策"],
    "How Google uses data":             ["How Google uses data","Jak Google používá data","Wie Google Daten verwendet","Cómo Google usa los datos","Comment Google utilise les données","Google डेटा का उपयोग कैसे करता है","Googleのデータの使用方法","Como o Google usa dados","Как Google использует данные","Google 如何使用数据"],
    "These cookies are necessary for the website to function and cannot be switched off.":[
        "These cookies are necessary for the website to function and cannot be switched off.","Tyto cookies jsou pro fungování webu nezbytné a nelze je vypnout.",
        "Diese Cookies sind notwendig für das Funktionieren der Website und können nicht deaktiviert werden.","Estas cookies son necesarias para que la web funcione y no pueden desactivarse.",
        "Ces cookies sont nécessaires au fonctionnement du site et ne peuvent pas être désactivés.","ये कुकीज़ वेबसाइट के कार्य के लिए आवश्यक हैं और इन्हें बंद नहीं किया जा सकता।",
        "これらのクッキーはウェブサイトの機能に必要であり、無効にできません。","Esses cookies são necessários para o funcionamento do site e não podem ser desativados.",
        "Эти файлы cookie необходимы для работы сайта и не могут быть отключены.","这些Cookie对网站运行是必需的，无法关闭。"
    ],
    "These cookies help us understand how visitors interact with our website by collecting and reporting information anonymously.":[
        "These cookies help us understand how visitors interact with our website by collecting and reporting information anonymously.","Tyto cookies nám pomáhají pochopit, jak návštěvníci interagují s naším webem, anonymním sběrem informací.",
        "Diese Cookies helfen uns zu verstehen, wie Besucher mit unserer Website interagieren, indem Informationen anonym gesammelt werden.","Estas cookies nos ayudan a entender cómo los visitantes interactúan con nuestro sitio recopilando información de forma anónima.",
        "Ces cookies nous aident à comprendre comment les visiteurs interagissent avec notre site en collectant des informations anonymement.","ये कुकीज़ हमें यह समझने में मदद करती हैं कि आगंतुक हमारी वेबसाइट के साथ कैसे इंटरैक्ट करते हैं।",
        "これらのクッキーは、訪問者がウェブサイトとどのようにやり取りしているかを匿名で収集することで理解するのに役立ちます。","Esses cookies nos ajudam a entender como os visitantes interagem com nosso site, coletando informações anonimamente.",
        "Эти файлы cookie помогают нам понять, как посетители взаимодействуют с сайтом, анонимно собирая информацию.","这些Cookie通过匿名收集和报告信息，帮助我们了解访问者如何与网站互动。"
    ],
    "These cookies are used to make advertising messages more relevant to you and your interests.":[
        "These cookies are used to make advertising messages more relevant to you and your interests.","Tyto cookies se používají k tomu, aby byly reklamní zprávy relevantnější pro vás a vaše zájmy.",
        "Diese Cookies werden verwendet, um Werbebotschaften relevanter für Sie zu machen.","Estas cookies se usan para hacer que los mensajes publicitarios sean más relevantes para usted.",
        "Ces cookies sont utilisés pour rendre les messages publicitaires plus pertinents.","ये कुकीज़ विज्ञापन संदेशों को आपकी रुचियों के लिए अधिक प्रासंगिक बनाने के लिए उपयोग की जाती हैं।",
        "これらのクッキーは、広告メッセージをお客様の興味に関連したものにするために使用されます。","Esses cookies são usados para tornar as mensagens publicitárias mais relevantes para você.",
        "Эти файлы cookie используются, чтобы сделать рекламные сообщения более релевантными для вас.","这些Cookie用于使广告信息更符合您的兴趣。"
    ],
    "We use cookies and similar technologies to improve your browsing experience, analyze website traffic, and understand where our visitors are coming from. By clicking 'Accept All', you consent to our use of cookies.":[
        "We use cookies and similar technologies to improve your browsing experience, analyze website traffic, and understand where our visitors are coming from. By clicking 'Accept All', you consent to our use of cookies.",
        "Používáme cookies a podobné technologie ke zlepšení vašeho prohlížení, analýze provozu a pochopení, odkud návštěvníci přicházejí. Kliknutím na 'Přijmout vše' souhlasíte s používáním cookies.",
        "Wir verwenden Cookies und ähnliche Technologien, um Ihr Surferlebnis zu verbessern, den Website-Traffic zu analysieren und zu verstehen, woher unsere Besucher kommen. Durch Klicken auf 'Alle akzeptieren' stimmen Sie der Verwendung von Cookies zu.",
        "Usamos cookies y tecnologías similares para mejorar su experiencia de navegación, analizar el tráfico del sitio y entender de dónde vienen nuestros visitantes. Al hacer clic en 'Aceptar todo', acepta el uso de cookies.",
        "Nous utilisons des cookies et des technologies similaires pour améliorer votre expérience de navigation, analyser le trafic du site et comprendre d'où viennent nos visiteurs. En cliquant sur 'Tout accepter', vous consentez à notre utilisation des cookies.",
        "हम आपके ब्राउज़िंग अनुभव को बेहतर बनाने, वेबसाइट ट्रैफ़िक का विश्लेषण करने और यह समझने के लिए कुकीज़ और समान तकनीकों का उपयोग करते हैं कि हमारे आगंतुक कहां से आ रहे हैं।",
        "当ウェブサイトでは、閲覧体験の向上、トラフィックの分析、訪問者の動向を理解するためにクッキーや類似技術を使用しています。「すべて受け入れる」をクリックすると、クッキーの使用に同意したことになります。",
        "Usamos cookies e tecnologias similares para melhorar sua experiência de navegação, analisar o tráfego do site e entender de onde vêm nossos visitantes. Ao clicar em 'Aceitar tudo', você consente com o uso de cookies.",
        "Мы используем файлы cookie и аналогичные технологии для улучшения работы сайта, анализа трафика и понимания, откуда приходят посетители. Нажимая 'Принять все', вы соглашаетесь на использование файлов cookie.",
        "我们使用Cookie和类似技术来改善您的浏览体验、分析网站流量并了解访问者的来源。点击『全部接受』即表示您同意我们使用Cookie。"
    ],

    # --- Admin / forms ---
    "Authentication":                   ["Authentication","Autentizace","Authentifizierung","Autenticación","Authentification","प्रमाणीकरण","認証","Autenticação","Аутентификация","身份验证"],
    "Manage Account":                   ["Manage Account","Spravovat účet","Konto verwalten","Administrar cuenta","Gérer le compte","खाता प्रबंधित करें","アカウント管理","Gerenciar conta","Управление учётной записью","管理账户"],
    "ContactUs":                        ["Contact Us","Kontakt","Kontakt","Contacto","Contact","संपर्क","お問い合わせ","Contato","Свяжитесь с нами","联系我们"],
    "Have Questions?":                  ["Have Questions?","Máte dotazy?","Haben Sie Fragen?","¿Tiene preguntas?","Des questions ?","प्रश्न हैं?","ご質問は？","Tem dúvidas?","Есть вопросы?","有疑问？"],
    "Our team of experts is ready to assist you with any inquiries":[
        "Our team of experts is ready to assist you with any inquiries","Náš tým odborníků je připraven vám pomoci s jakýmikoli dotazy",
        "Unser Expertenteam steht Ihnen bei allen Anfragen zur Verfügung","Nuestro equipo de expertos está listo para ayudarle con cualquier consulta",
        "Notre équipe d'experts est prête à vous aider pour toute demande","हमारी विशेषज्ञ टीम किसी भी पूछताछ में आपकी सहायता के लिए तैयार है",
        "弊社の専門チームがあらゆるお問い合わせに対応いたします","Nossa equipe de especialistas está pronta para ajudá-lo com qualquer consulta",
        "Наша команда экспертов готова помочь вам по любым вопросам","我们的专家团队随时准备回答您的任何询问"
    ],
    "Fill out the form below and our specialist will contact you within 24 hours.":[
        "Fill out the form below and our specialist will contact you within 24 hours.","Vyplňte níže uvedený formulář a náš specialista vás bude kontaktovat do 24 hodin.",
        "Füllen Sie das folgende Formular aus und unser Spezialist wird sich innerhalb von 24 Stunden bei Ihnen melden.","Complete el formulario a continuación y nuestro especialista se pondrá en contacto dentro de 24 horas.",
        "Remplissez le formulaire ci-dessous et notre spécialiste vous contactera dans les 24 heures.","नीचे दिया गया फ़ॉर्म भरें और हमारे विशेषज्ञ 24 घंटे के भीतर आपसे संपर्क करेंगे।",
        "以下のフォームにご記入ください。専門スタッフより24時間以内にご連絡いたします。","Preencha o formulário abaixo e nosso especialista entrará em contato em até 24 horas.",
        "Заполните форму ниже, и наш специалист свяжется с вами в течение 24 часов.","填写下面的表格，我们的专家将在24小时内与您联系。"
    ],
    "News & Insights":                  ["News & Insights","Novinky a postřehy","Nachrichten & Einblicke","Noticias e información","Actualités et analyses","समाचार और अंतर्दृष्टि","ニュースと洞察","Notícias e Insights","Новости и аналитика","新闻与见解"],
    "Stay informed about the world of luxury collectibles and fine art":[
        "Stay informed about the world of luxury collectibles and fine art","Zůstaňte informováni o světě luxusních sběratelských předmětů a výtvarného umění",
        "Bleiben Sie informiert über die Welt der Luxus-Sammlerstücke und der bildenden Kunst","Manténgase informado sobre el mundo de los coleccionables de lujo y el arte",
        "Restez informé du monde des objets de collection de luxe et des beaux-arts","लक्ज़री संग्रहणीय और ललित कला की दुनिया के बारे में सूचित रहें",
        "高級コレクティブルとファインアートの世界について常に最新情報を入手","Fique informado sobre o mundo dos colecionáveis de luxo e belas-artes",
        "Будьте в курсе мира роскошных коллекционных предметов и искусства","了解奢华收藏品和美术界的最新动态"
    ],
    "Our Story":                        ["Our Story","Náš příběh","Unsere Geschichte","Nuestra historia","Notre histoire","हमारी कहानी","私たちの物語","Nossa história","Наша история","我们的故事"],
    "Our Promise":                      ["Our Promise","Náš slib","Unser Versprechen","Nuestra promesa","Notre promesse","हमारा वादा","私たちの約束","Nossa promessa","Наше обещание","我们的承诺"],
    "What We Offer":                    ["What We Offer","Co nabízíme","Was wir bieten","Qué ofrecemos","Ce que nous offrons","हम क्या प्रदान करते हैं","私たちの提供内容","O que oferecemos","Что мы предлагаем","我们提供什么"],
    "Client Rating":                    ["Client Rating","Hodnocení klientů","Kundenbewertung","Calificación del cliente","Évaluation client","ग्राहक रेटिंग","顧客評価","Avaliação do cliente","Оценка клиентов","客户评分"],
    "Years Experience":                 ["Years Experience","Let zkušeností","Jahre Erfahrung","Años de experiencia","Années d'expérience","वर्षों का अनुभव","年の経験","Anos de experiência","Лет опыта","年经验"],
    "Items Sold":                       ["Items Sold","Prodaných položek","Verkaufte Artikel","Artículos vendidos","Articles vendus","बेची गई वस्तुएं","販売商品","Itens vendidos","Продано предметов","已售商品"],
}

def find_existing_keys(file_path):
    """Return set of existing keys in a .resx file."""
    with open(file_path, encoding='utf-8') as f:
        content = f.read()
    return set(re.findall(r'<data name="([^"]+)"', content))

def xml_escape(value):
    """Escape XML special characters in resx value."""
    return value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

def xml_escape_key(key):
    """Escape key for XML attribute."""
    return key.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')

def add_missing_entries(lang):
    lang_idx = LANGS.index(lang)
    path = file_for(lang)
    if not path.exists():
        print(f"MISSING FILE: {path}")
        return 0
    existing = find_existing_keys(path)
    with open(path, encoding='utf-8') as f:
        content = f.read()
    
    new_lines = []
    added = 0
    for key, translations in TRANSLATIONS.items():
        if key in existing:
            continue
        val = translations[lang_idx]
        new_lines.append(f'<data name="{xml_escape_key(key)}" xml:space="preserve"><value>{xml_escape(val)}</value></data>')
        added += 1
    
    if added == 0:
        print(f"{lang}: nothing to add")
        return 0
    
    # Insert before </root>
    insertion = "\n<!-- Auto-added missing translations -->\n" + "\n".join(new_lines) + "\n"
    new_content = content.replace('</root>', insertion + '</root>')
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print(f"{lang}: added {added} keys")
    return added

def main():
    total = 0
    for lang in LANGS:
        total += add_missing_entries(lang)
    print(f"\nTotal keys added across all files: {total}")

if __name__ == "__main__":
    main()
