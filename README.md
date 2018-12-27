# PointCloudViewer

これは .pts 形式で作成されたPoint cloudデータをUnity上で閲覧するためのスクリプトと、そのUnityプロジェクトです。
Taskを使っているので、非対応のUnityバージョンでは利用できません。

作成時は2018.2.18f1を使用しています。

動作の保証等は致しませんが、私的利用の範囲内でご自由にご利用ください。

# HOW TO USE

cubingというシーンがサンプル用に入っています。

cubingにはいくつかオブジェクトがありますが、触る必要があるのはArrangingだけです。（必要に応じてCameraは削除しても大丈夫です。）

Arrangingにはいくつかのスクリプトがアタッチされていますが、適当に役割とパラメータの解説をしていきます。

## Pts To Cubing Manager
 ### Pts To Cubing Manager は 指定されたptsデータをUnity上で任意の形状に置き換えモデル化する処理です
 ### これ自体に機能はなく、それぞれの機能を呼び出したり、管理したりします
 + File Path
  + .ptsファイルのパスを指定します。Projectフォルダをルートとした相対パスか、絶対パスでの記述が使用できます。
 + Cube Size
  + 点群を変換する際、何メートル間隔で点群をまとめるか指定します。
  + この指定を「1」にすると、XYZ軸共に-0.5~0.5mの正方形状に指定される範囲内にある点がすべて1つの点として処理されます。
 + Max Thread Num
  + 使用されるスレッドの最大数
 + Max Vertex Count In A Mesh (will deprecate)
 + スクリプト系
  + Converter
  + Arranger
  + Slicer
  + Baker
  + Saver
  + Cuber
  + Pb Manager
  + Pb Manager Activer Manager
  + subpb Manager
  + Sub PB Manager Active Manager
 + State Text
  + 今どのスクリプトが処理しているかを把握するための表示領域
  
## Pts To Cloud Point Converter
 ### 
  
  
