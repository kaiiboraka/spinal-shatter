@echo off
set "BASE_PATH=assets\Resources\ShopItemData\Spells\Orb\RankData"
set "RANKUP_SCRIPT_PATH=res://Scripts/Resources/Items/RankUpData.cs"

for /L %%i in (1,1,9) do (
    rem Create Orb_P_Rank%%i.tres
    echo [gd_resource type="Resource" script_class="RankUpData" load_steps=2 format=3]> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo.>> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo [ext_resource type="Script" path="%RANKUP_SCRIPT_PATH%" id="1_rankup"]>> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo.>> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo [resource]>> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo script = ExtResource("1_rankup")>> "%BASE_PATH%\Orb_P_Rank%%i.tres"
    echo StatModifiers = Dictionary[int, float]({})>> "%BASE_PATH%\Orb_P_Rank%%i.tres"

    rem Create Orb_Alt_Rank%%i.tres
    echo [gd_resource type="Resource" script_class="RankUpData" load_steps=2 format=3]> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo.>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo [ext_resource type="Script" path="%RANKUP_SCRIPT_PATH%" id="1_rankup"]>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo.>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo [resource]>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo script = ExtResource("1_rankup")>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
    echo StatModifiers = Dictionary[int, float]({})>> "%BASE_PATH%\Orb_Alt_Rank%%i.tres"
)